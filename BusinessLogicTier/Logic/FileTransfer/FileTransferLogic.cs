using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.CertificateGenerator;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.FileTransferFramework;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;

using log4net;

using Renci.SshNet.Common;

namespace HEAppE.BusinessLogicTier.logic.FileTransfer
{
    public class FileTransferLogic : IFileTransferLogic
    {
        #region Instances
        /// <summary>
        /// Unit of work
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;
        /// <summary>
        /// _logger
        /// </summary>
        private readonly ILog _log;

        /// <summary>
        /// Script Configuration
        /// </summary>
        protected readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="unitOfWork">Unit of work</param>
        public FileTransferLogic(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region Methods
        public void RemoveJobsTemporaryFileTransferKeys()
        {
            var activeTemporaryKeys = _unitOfWork.FileTransferTemporaryKeyRepository.GetAllActiveTemporaryKey()
                    .Where(w => w.AddedAt.AddHours(BusinessLogicConfiguration.ValidityOfTemporaryTransferKeysInHours) <= DateTime.UtcNow)
                     .ToList();

            var activeTemporaryKeysGroup = activeTemporaryKeys.GroupBy(g => g.SubmittedJob.Specification.Cluster)
                                                                                                        .ToList();

            foreach (var activeTemporaryKeyGroup in activeTemporaryKeysGroup)
            {
                Cluster cluster = activeTemporaryKeyGroup.Key;

                var clusterUserActiveTempKey = activeTemporaryKeyGroup.GroupBy(g =>
                        new { g.SubmittedJob.Specification.ClusterUser, g.SubmittedJob.Specification.Cluster, g.SubmittedJob.Specification.Project })
                    .ToList();

                foreach (var tempKey in clusterUserActiveTempKey)
                {
                    var scheduler = SchedulerFactory.GetInstance(cluster.SchedulerType)
                        .CreateScheduler(cluster, tempKey.Key.Project);
                    scheduler.RemoveDirectFileTransferAccessForUser(tempKey.Select(s => s.PublicKey),
                        tempKey.Key.ClusterUser, tempKey.Key.Cluster);
                }

                activeTemporaryKeyGroup.ToList().ForEach(f => f.IsDeleted = true);
                _unitOfWork.Save();
            }
        }

        public FileTransferMethod TrustfulRequestFileTransfer(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            _log.Info($"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);

            var clusterUserAuthCredentials = jobInfo.Specification.ClusterUser;
            if (string.IsNullOrEmpty(clusterUserAuthCredentials.PrivateKey))
            {
                throw new ClusterAuthenticationException("NotExistingPrivateKey", clusterUserAuthCredentials.PrivateKey);
            }

            var transferMethod = new FileTransferMethod
            {
                Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
                Port = jobInfo.Specification.FileTransferMethod.Port,
                Cluster = jobInfo.Specification.Cluster,
                ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
                SharedBasePath = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.SubExecutionsPath),
                Credentials = new FileTransferKeyCredentials
                {
                    Username = clusterUserAuthCredentials.Username,
                    Password = clusterUserAuthCredentials.Password,
                    FileTransferCipherType = clusterUserAuthCredentials.CipherType,
                    PrivateKey = clusterUserAuthCredentials.PrivateKey,
                    PrivateKeyCertificate = clusterUserAuthCredentials.PrivateKeyCertificate,
                    Passphrase = clusterUserAuthCredentials.PrivateKeyPassphrase
                }
            };
            return transferMethod;
        }

        public FileTransferMethod GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            _log.Info($"Getting file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            Cluster cluster = jobInfo.Specification.Cluster;

            if (jobInfo.FileTransferTemporaryKeys.Count(c => !c.IsDeleted) > BusinessLogicConfiguration.GeneratedFileTransferKeyLimitPerJob)
            {
                throw new FileTransferTemporaryKeyException("SshKeyGenerationLimit");
            }

            string publicKey = string.Empty;
            FileTransferMethod transferMethod = new FileTransferMethod
            {
                Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
                Port = jobInfo.Specification.FileTransferMethod.Port,
                Cluster = jobInfo.Specification.Cluster,
                ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
                SharedBasePath = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification, _scripts.SubExecutionsPath)
            };

            _log.Info($"Auth type: {jobInfo.Specification.ClusterUser.AuthenticationType}");
            if (jobInfo.Specification.ClusterUser.AuthenticationType == ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)
            {
                var credentials = _unitOfWork.ClusterAuthenticationCredentialsRepository.GetById(jobInfo.Specification.ClusterUser.Id);
                _log.Debug($"ClusterUser: {credentials}");
                transferMethod.Credentials = new FileTransferKeyCredentials
                {
                    Username = jobInfo.Specification.ClusterUser.Username,
                    FileTransferCipherType = credentials.CipherType,
                    PrivateKey = credentials.PrivateKey,
                    PrivateKeyCertificate = credentials.PrivateKeyCertificate,
                    PublicKey = credentials.PublicKeyFingerprint
                };
                return transferMethod;
            }


            var certGenerator = new SSHGenerator();
            publicKey = certGenerator.ToPuTTYPublicKey();

            while (_unitOfWork.FileTransferTemporaryKeyRepository.ContainsActiveTemporaryKey(publicKey))
            {
                certGenerator.Regenerate();
                publicKey = certGenerator.ToPuTTYPublicKey();
            }

            transferMethod.Credentials = new FileTransferKeyCredentials
            {
                Username = jobInfo.Specification.ClusterUser.Username,
                FileTransferCipherType = certGenerator.CipherType,
                PrivateKey = certGenerator.ToPrivateKey(),
                PublicKey = publicKey
            };


            jobInfo.FileTransferTemporaryKeys.Add(
                new FileTransferTemporaryKey()
                {
                    AddedAt = DateTime.UtcNow,
                    PublicKey = publicKey,
                });

            SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project).AllowDirectFileTransferAccessForUserToJob(publicKey, jobInfo);

            _unitOfWork.Save();
            return transferMethod;
        }

        public void EndFileTransfer(long submittedJobInfoId, string publicKey, AdaptorUser loggedUser)
        {
            _log.Info($"Removing file transfer method for submitted job Id \"{submittedJobInfoId}\" with user \"{loggedUser.GetLogIdentification()}\"");
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            Cluster cluster = jobInfo.Specification.Cluster;

            if (jobInfo.Specification.ClusterUser.AuthenticationType is ClusterAuthenticationCredentialsAuthType.PrivateKeyInVaultAndInSshAgent)
            {
                return;
            }

            var temporaryKey = jobInfo.FileTransferTemporaryKeys.Find(f => f.PublicKey == publicKey);

            if (temporaryKey is null)
            {
                throw new FileTransferTemporaryKeyException("PublicKeyMismatch");
            }

            SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster, jobInfo.Project).RemoveDirectFileTransferAccessForUser(
                new string[] { temporaryKey.PublicKey }, temporaryKey.SubmittedJob.Specification.ClusterUser, jobInfo.Specification.Cluster);

            temporaryKey.IsDeleted = true;
            _unitOfWork.Save();
        }

        public IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser)
        {
            _log.Info($"Getting part of job files from cluster for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            IRexFileSystemManager fileManager =
                    FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol).CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);
            IList<JobFileContent> result = new List<JobFileContent>();
            foreach (SubmittedTaskInfo taskInfo in jobInfo.Tasks)
            {
                IList<TaskFileOffset> currentTaskFileOffsets = (from taskFileOffset in taskFileOffsets where taskFileOffset.SubmittedTaskInfoId == taskInfo.Id select taskFileOffset).ToList();
                foreach (TaskFileOffset currentOffset in currentTaskFileOffsets)
                {
                    ICollection<JobFileContent> contents = fileManager.DownloadPartOfJobFileFromCluster(taskInfo, currentOffset.FileType, currentOffset.Offset);
                    if (contents != null)
                    {
                        foreach (JobFileContent content in contents)
                        {
                            result.Add(content);
                        }
                    }
                }
            }
            return result;
        }

        public IList<SynchronizedJobFiles> SynchronizeAllUnfinishedJobFiles()
        {
            var unfinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetNotFinishedJobInfos().ToList();

            IEnumerable<IGrouping<FileTransferMethod, SubmittedJobInfo>> fileTransferMethodGroups =
                (from jobInfo in unfinishedJobs group jobInfo by jobInfo.Specification.FileTransferMethod into fileTransferMethodGroup select fileTransferMethodGroup);
            IList<SynchronizedJobFiles> result = new List<SynchronizedJobFiles>(unfinishedJobs.Count);

            foreach (var fileTransferMethodGroup in fileTransferMethodGroups)
            {
                IRexFileSystemManager fileManager = FileSystemFactory.GetInstance(fileTransferMethodGroup.Key.Protocol).CreateFileSystemManager(fileTransferMethodGroup.Key);
                foreach (var jobInfo in fileTransferMethodGroup)
                {
                    DateTime synchronizationTime = DateTime.UtcNow;
                    ICollection<JobFileContent> files = fileManager.CopyLogFilesFromCluster(jobInfo);
                    foreach (JobFileContent file in fileManager.CopyProgressFilesFromCluster(jobInfo))
                    {
                        files.Add(file);
                    }
                    foreach (JobFileContent file in fileManager.CopyStdOutputFilesFromCluster(jobInfo))
                    {
                        files.Add(file);
                    }
                    foreach (JobFileContent file in fileManager.CopyStdErrorFilesFromCluster(jobInfo))
                    {
                        files.Add(file);
                    }
                    SynchronizedJobFiles fileContents = new SynchronizedJobFiles
                    {
                        SubmittedJobInfoId = jobInfo.Id,
                        SynchronizationTime = synchronizationTime,
                        FileContents = files.ToList()
                    };
                    result.Add(fileContents);
                }
            }
            return result;
        }

        public ICollection<FileInformation> ListChangedFilesForJob(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
                return null;
            IRexFileSystemManager fileManager =
                    FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol).CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);

            return fileManager.ListChangedFilesForJob(jobInfo, jobInfo.SubmitTime.Value);
        }

        public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser)
        {
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
                return null;

            IRexFileSystemManager fileManager =
                    FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol).CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);
            try
            {
                //if the path does not start with job id and then task id
                if (!Regex.Match(relativeFilePath, @"^\/?[0-9]+\/[0-9]+\/").Success)
                {
                    if (relativeFilePath.StartsWith("/"))
                    {
                        relativeFilePath = relativeFilePath[1..];
                    }
                    relativeFilePath = Path.Combine($"{jobInfo.Id}/", relativeFilePath);
                }

                relativeFilePath = Path.Combine("/", relativeFilePath);
                return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath);
            }
            catch (SftpPathNotFoundException exception)
            {
                throw new InvalidRequestException("NotExistingPath", relativeFilePath, exception.Message);
            }
        }

        public virtual FileTransferMethod GetFileTransferMethodById(long fileTransferMethodById)
        {
            FileTransferMethod fileTransferMethod = _unitOfWork.FileTransferMethodRepository.GetById(fileTransferMethodById);
            return fileTransferMethod == null
                ? throw new RequestedObjectDoesNotExistException("NotExistingFileTransferMethod", fileTransferMethodById)
                : fileTransferMethod;
        }

        public virtual IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId)
        {
            return _unitOfWork.FileTransferMethodRepository.GetByClusterId(clusterId)
                                                            .ToList();
        }
        #endregion
    }
}
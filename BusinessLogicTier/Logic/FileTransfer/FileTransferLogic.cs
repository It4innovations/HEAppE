using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.FileTransferFramework;
using log4net;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;
using Renci.SshNet.Common;
using HEAppE.HpcConnectionFramework.SchedulerAdapters;
using HEAppE.Utils;
using HEAppE.CertificateGenerator;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;

namespace HEAppE.BusinesslogicTier.logic.FileTransfer
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
        public FileTransferMethod GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            _log.Info($"Getting file transfer method for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            Cluster cluster = jobInfo.Specification.Cluster;

            // TODO try catch block
            // TODO add this value to configuration
            if (jobInfo.FileTransferTemporaryKeys.Count > 5)
            {
                //TODO change exception type
                throw new Exception("It was reach the job limit of generated ssh keys for direct transfer!");
            }

            var certGenerator = new SSHGenerator();
            string publicKey = certGenerator.ToPuTTYPublicKey();

            while (_unitOfWork.FileTransferTemporaryKeyRepository.ContainsActiveTemporaryKey(publicKey))
            {
                certGenerator = new SSHGenerator();
                publicKey = certGenerator.ToPuTTYPublicKey();
            }

            var transferMethod = new FileTransferMethod
            {
                Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
                ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
                SharedBasePath = FileSystemUtils.GetJobClusterDirectoryPath(cluster.LocalBasepath, jobInfo.Specification),
                Credentials = new AsymmetricKeyCredentials
                {
                    Username = jobInfo.Specification.ClusterUser.Username,
                    PrivateKey = certGenerator.ToPrivateKey(),
                    PublicKey = publicKey
                }
            };

            jobInfo.FileTransferTemporaryKeys.Add(
                new FileTransferTemporaryKey()
                {
                    AddedAt = DateTime.UtcNow,
                    PublicKey = publicKey,
                });

            SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).AllowDirectFileTransferAccessForUserToJob(publicKey, jobInfo);

            _unitOfWork.Save();
            return transferMethod;
        }

        public void EndFileTransfer(long submittedJobInfoId, FileTransferMethod transferMethod, AdaptorUser loggedUser)
        {
            _log.Info($"Removing file transfer method for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(_unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            Cluster cluster = jobInfo.Specification.Cluster;

            //TODO try catch


            AsymmetricKeyCredentials asymmetricKeyCredentials = transferMethod.Credentials as AsymmetricKeyCredentials;
            if (asymmetricKeyCredentials is null)
            {
                var errMessage = $"Credentials of class {transferMethod.Credentials.GetType().Name} are not supported!";
                _log.Error(errMessage);
                throw new ArgumentException(errMessage);
            }

            var temporaryKey = jobInfo.FileTransferTemporaryKeys.Find(f => f.PublicKey == asymmetricKeyCredentials.PublicKey);
            if (temporaryKey is null)
            {
                throw new Exception("");
            }

            SchedulerFactory.GetInstance(cluster.SchedulerType).CreateScheduler(cluster).RemoveDirectFileTransferAccessForUserToJob(asymmetricKeyCredentials.PublicKey, jobInfo);
            temporaryKey.IsDeleted = true;
            _unitOfWork.Save();
        }

        public IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser)
        {
            _log.Info("Getting part of job files from cluster for submitted job Id {submittedJobInfoId} with user {loggedUser.GetLogIdentification()}");
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
                IRexFileSystemManager fileManager =
                    FileSystemFactory.GetInstance(fileTransferMethodGroup.Key.Protocol).CreateFileSystemManager(fileTransferMethodGroup.Key);
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
                return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath);
            }
            catch (SftpPathNotFoundException exception)
            {
                _log.Warn($"{loggedUser} is requesting not existing file '{relativeFilePath}'");
                ExceptionHandler.ThrowProperExternalException(new InvalidRequestException(exception.Message));
            }

            return null;
        }

        public virtual FileTransferMethod GetFileTransferMethodById(long fileTransferMethodById)
        {
            FileTransferMethod fileTransferMethod = _unitOfWork.FileTransferMethodRepository.GetById(fileTransferMethodById);
            if (fileTransferMethod == null)
            {
                _log.Error("Requested FileTransferMethod with Id=" + fileTransferMethodById + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested FileTransferMethod with Id=" + fileTransferMethodById + " does not exist in the system.");
            }
            return fileTransferMethod;
        }

        public virtual IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId)
        {
            return _unitOfWork.FileTransferMethodRepository.GetByClusterId(clusterId)
                                                            .ToList();
        }
        #endregion
    }
}
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
using HEAppE.HpcConnectionFramework;
using HEAppE.MiddlewareUtils;
using log4net;
using HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions;

namespace HEAppE.BusinessLogicTier.Logic.FileTransfer
{
    internal class FileTransferLogic : IFileTransferLogic
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected readonly IUnitOfWork unitOfWork;

        internal FileTransferLogic(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }

        public FileTransferMethod GetFileTransferMethod(long submittedJobInfoId, AdaptorUser loggedUser)
        {
            log.Info("Getting file transfer method for submitted job info ID " + submittedJobInfoId + " with user " + loggedUser.GetLogIdentification());
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            var certificateGenerator = new CertificateGenerator.CertificateGenerator();
            certificateGenerator.GenerateKey(2048);
            string publicKey = certificateGenerator.DhiPublicKey();
            string jobDir = FileSystemUtils.GetJobClusterDirectoryPath(jobInfo.Specification.FileTransferMethod.Cluster.LocalBasepath, jobInfo.Specification);
            var transferMethod = new FileTransferMethod
            {
                Protocol = jobInfo.Specification.FileTransferMethod.Protocol,
                ServerHostname = jobInfo.Specification.FileTransferMethod.ServerHostname,
                SharedBasePath = jobDir,
                Credentials = new AsymmetricKeyCredentials
                {
                    Username = jobInfo.Specification.ClusterUser.Username,
                    PrivateKey = certificateGenerator.DhiPrivateKey(),
                    PublicKey = publicKey
                }
            };

            SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster).AllowDirectFileTransferAccessForUserToJob(publicKey, jobInfo);
            return transferMethod;
        }

        public void EndFileTransfer(long submittedJobInfoId, FileTransferMethod transferMethod, AdaptorUser loggedUser)
        {
            log.Info("Removing file transfer method for submitted job info ID " + submittedJobInfoId + " with user " + loggedUser.GetLogIdentification());
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            AsymmetricKeyCredentials asymmetricKeyCredentials = transferMethod.Credentials as AsymmetricKeyCredentials;
            if (asymmetricKeyCredentials != null)
                SchedulerFactory.GetInstance(jobInfo.Specification.Cluster.SchedulerType).CreateScheduler(jobInfo.Specification.Cluster).
                    RemoveDirectFileTransferAccessForUserToJob(asymmetricKeyCredentials.PublicKey, jobInfo);
            else
            {
                log.Error("Credentials of class " + transferMethod.Credentials.GetType().Name +
                          " are not supported. Change the HaaSMiddleware.BusinessLogicTier.FileTransfer.FileTransferLogic.EndFileTransfer() method to add support for additional credential types.");
                throw new ArgumentException("Credentials of class " + transferMethod.Credentials.GetType().Name +
                                            " are not supported. Change the HaaSMiddleware.BusinessLogicTier.FileTransfer.FileTransferLogic.EndFileTransfer() method to add support for additional credential types.");
            }
        }

        public IList<JobFileContent> DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffset[] taskFileOffsets, AdaptorUser loggedUser)
        {
            log.Info("Getting part of job files from cluster for submitted job info ID " + submittedJobInfoId + " with user " + loggedUser.GetLogIdentification());
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
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
            IList<SubmittedJobInfo> unfinishedJobs = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).ListNotFinishedJobInfos();

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
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
                return null;
            IRexFileSystemManager fileManager =
                    FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol).CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);
            return fileManager.ListChangedFilesForJob(jobInfo, jobInfo.SubmitTime.Value);
        }

        public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, AdaptorUser loggedUser)
        {
            SubmittedJobInfo jobInfo = LogicFactory.GetLogicFactory().CreateJobManagementLogic(unitOfWork).GetSubmittedJobInfoById(submittedJobInfoId, loggedUser);
            if (jobInfo.State < JobState.Submitted || jobInfo.State == JobState.WaitingForServiceAccount)
                return null;
            IRexFileSystemManager fileManager =
                    FileSystemFactory.GetInstance(jobInfo.Specification.FileTransferMethod.Protocol).CreateFileSystemManager(jobInfo.Specification.FileTransferMethod);
            return fileManager.DownloadFileFromCluster(jobInfo, relativeFilePath);
        }

        public virtual FileTransferMethod GetFileTransferMethodById(long fileTransferMethodById)
        {
            FileTransferMethod fileTransferMethod = unitOfWork.FileTransferMethodRepository.GetById(fileTransferMethodById);
            if (fileTransferMethod == null)
            {
                log.Error("Requested FileTransferMethod with Id=" + fileTransferMethodById + " does not exist in the system.");
                throw new RequestedObjectDoesNotExistException("Requested FileTransferMethod with Id=" + fileTransferMethodById + " does not exist in the system.");
            }
            return fileTransferMethod;
        }

        public virtual IEnumerable<FileTransferMethod> GetFileTransferMethodsByClusterId(long clusterId)
        {
            return unitOfWork.FileTransferMethodRepository.GetByClusterId(clusterId)
                                                    .ToList();
        }
        
    }
}
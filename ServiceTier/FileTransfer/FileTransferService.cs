using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace HEAppE.ServiceTier.FileTransfer
{
    public class FileTransferService : IFileTransferService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FileTransferMethodExt TrustfulRequestFileTransfer(long submittedJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
                if (submittedJobInfo == null)
                {
                    throw new InputValidationException($"SubmittedJobInfo with id '{submittedJobInfoId}' not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
                IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                FileTransferMethod fileTransferMethod = fileTransferLogic.TrustfulRequestFileTransfer(submittedJobInfoId, loggedUser);
                return fileTransferMethod.ConvertIntToExt();
            }
        }

        public FileTransferMethodExt RequestFileTransfer(long submittedJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
                if (submittedJobInfo == null)
                {
                    throw new InputValidationException($"SubmittedJobInfo with id '{submittedJobInfoId}' not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
                IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                FileTransferMethod fileTransferMethod = fileTransferLogic.GetFileTransferMethod(submittedJobInfoId, loggedUser);
                return fileTransferMethod.ConvertIntToExt();
            }
        }

        public void CloseFileTransfer(long submittedJobInfoId, string publicKey, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
                if (submittedJobInfo == null)
                {
                    throw new InputValidationException($"SubmittedJobInfo with id '{submittedJobInfoId}' not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
                IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                fileTransferLogic.EndFileTransfer(submittedJobInfoId, publicKey, loggedUser);
            }
        }

        public JobFileContentExt[] DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId, TaskFileOffsetExt[] taskFileOffsets, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
                if (submittedJobInfo == null)
                {
                    throw new InputValidationException($"SubmittedJobInfo with id '{submittedJobInfoId}' not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
                IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                IList<JobFileContent> downloadedFileParts = fileTransferLogic.DownloadPartsOfJobFilesFromCluster(
                    submittedJobInfoId,
                    (from taskFileOffset in (new List<TaskFileOffsetExt>(taskFileOffsets).ToList()) select FileTransferConverts.ConvertTaskFileOffsetExtToInt(taskFileOffset)).ToArray(),
                    loggedUser);
                return (from fileContent in downloadedFileParts select FileTransferConverts.ConvertJobFileContentToExt(fileContent)).ToArray();
            }
        }

        public FileInformationExt[] ListChangedFilesForJob(long submittedJobInfoId, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
                if (submittedJobInfo == null)
                {
                    throw new InputValidationException($"SubmittedJobInfo with id '{submittedJobInfoId}' not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
                IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                ICollection<FileInformation> result = fileTransferLogic.ListChangedFilesForJob(submittedJobInfoId, loggedUser);
                return result?.Select(s => s.ConvertIntToExt()).ToArray();
            }
        }

        public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, string sessionCode)
        {
            using (IUnitOfWork unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
            {
                var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId);
                if (submittedJobInfo == null)
                {
                    throw new InputValidationException($"SubmittedJobInfo with id '{submittedJobInfoId}' not found");
                }
                AdaptorUser loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
                IFileTransferLogic fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork);
                return fileTransferLogic.DownloadFileFromCluster(submittedJobInfoId, relativeFilePath, loggedUser);
            }
        }
    }
}
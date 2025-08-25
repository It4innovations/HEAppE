using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using log4net;
using SshCaAPI;

namespace HEAppE.ServiceTier.FileTransfer;

public class FileTransferService : IFileTransferService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    
    public FileTransferService(ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
    }

    public FileTransferMethodExt TrustfulRequestFileTransfer(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService);
            var fileTransferMethod = fileTransferLogic.TrustfulRequestFileTransfer(submittedJobInfoId, loggedUser);
            return fileTransferMethod.ConvertIntToExt();
        }
    }

    public FileTransferMethodExt RequestFileTransfer(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService);
            var fileTransferMethod = fileTransferLogic.GetFileTransferMethod(submittedJobInfoId, loggedUser);
            return fileTransferMethod.ConvertIntToExt();
        }
    }

    public void CloseFileTransfer(long submittedJobInfoId, string publicKey, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService);
            fileTransferLogic.EndFileTransfer(submittedJobInfoId, publicKey, loggedUser);
        }
    }

    public JobFileContentExt[] DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId,
        TaskFileOffsetExt[] taskFileOffsets, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService);
            var downloadedFileParts = fileTransferLogic.DownloadPartsOfJobFilesFromCluster(
                submittedJobInfoId,
                (from taskFileOffset in new List<TaskFileOffsetExt>(taskFileOffsets).ToList()
                    select FileTransferConverts.ConvertTaskFileOffsetExtToInt(taskFileOffset)).ToArray(),
                loggedUser);
            return (from fileContent in downloadedFileParts
                select FileTransferConverts.ConvertJobFileContentToExt(fileContent)).ToArray();
        }
    }

    public FileInformationExt[] ListChangedFilesForJob(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService);
            var result = fileTransferLogic.ListChangedFilesForJob(submittedJobInfoId, loggedUser);
            return result?.Select(s => s.ConvertIntToExt()).ToArray();
        }
    }

    public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
                AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _sshCertificateAuthorityService);
            return fileTransferLogic.DownloadFileFromCluster(submittedJobInfoId, relativeFilePath, loggedUser);
        }
    }
}
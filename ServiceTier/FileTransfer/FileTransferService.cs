using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Renci.SshNet;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.FileTransfer.Converts;
using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.Services.UserOrg;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.ServiceTier.FileTransfer;

public class FileTransferService : IFileTransferService
{
    private readonly ILogger _logger;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IUserOrgService _userOrgService;
    
    public FileTransferService(IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys, ILogger logger)
    {
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _logger = logger;
    }

    public async Task<FileTransferMethodExt> TrustfulRequestFileTransfer(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            var fileTransferMethod = await fileTransferLogic.TrustfulRequestFileTransfer(submittedJobInfoId, loggedUser);
            return fileTransferMethod.ConvertIntToExt();
        }
    }

    public async Task<FileTransferMethodExt> RequestFileTransfer(long submittedJobInfoId, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            var fileTransferMethod = await fileTransferLogic.GetFileTransferMethod(submittedJobInfoId, loggedUser);
            return fileTransferMethod.ConvertIntToExt();
        }
    }

    public void CloseFileTransfer(long submittedJobInfoId, string publicKey, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            fileTransferLogic.EndFileTransfer(submittedJobInfoId, publicKey, loggedUser);
        }
    }

    public JobFileContentExt[] DownloadPartsOfJobFilesFromCluster(long submittedJobInfoId,
        TaskFileOffsetExt[] taskFileOffsets, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
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
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            var result = fileTransferLogic.ListChangedFilesForJob(submittedJobInfoId, loggedUser);
            return result?.Select(s => s.ConvertIntToExt()).ToArray();
        }
    }

    public byte[] DownloadFileFromCluster(long submittedJobInfoId, string relativeFilePath, string sessionCode)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var submittedJobInfo = unitOfWork.SubmittedJobInfoRepository.GetById(submittedJobInfoId) ??
                                   throw new InputValidationException("NotExistingSubmittedJobInfo",
                                       submittedJobInfoId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                _logger, AdaptorUserRoleType.Submitter, submittedJobInfo.Project.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            return fileTransferLogic.DownloadFileFromCluster(submittedJobInfoId, relativeFilePath, loggedUser);
        }
    }
    
    public async Task<dynamic> UploadFileToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId, string sessionCode)
    {
        await Task.Delay(1);
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var project = unitOfWork.ProjectRepository.GetById(projectId)
            ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                            _logger, AdaptorUserRoleType.Manager, projectId);

            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            return fileTransferLogic.UploadFileToProjectDir(fileStream, fileName, projectId, clusterId, loggedUser);
        }
    }
    
    public async Task<dynamic> UploadJobScriptToProjectDir(Stream fileStream, string fileName, long projectId, long clusterId, string sessionCode)
    {
        await Task.Delay(1);
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var project = unitOfWork.ProjectRepository.GetById(projectId)
            ?? throw new RequestedObjectDoesNotExistException("ProjectNotFound", projectId);

            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                            _logger, AdaptorUserRoleType.Manager, projectId);

            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            return fileTransferLogic.UploadJobScriptToProjectDir(fileStream, fileName, projectId, clusterId, loggedUser);
        }
    }
    
    public async Task<dynamic> UploadFileToJobExecutionDir(Stream fileStream, string fileName, long createdJobInfoId, long? createdTaskInfoId, string sessionCode)
    {
        await Task.Delay(1);
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger))
        {
            var job = unitOfWork.JobSpecificationRepository.GetById(createdJobInfoId) ??
                      throw new InputValidationException("NotExistingJob", createdJobInfoId);
            // Validate that the task belongs to the job
            if(createdTaskInfoId.HasValue)
            {
                if(job.Tasks.Any(t => t.Id == createdTaskInfoId.Value) == false)
                {
                    throw new InputValidationException("TaskDoesNotBelongToJob", createdTaskInfoId.Value);
                }
            }
            var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _userOrgService,  _sshCertificateAuthorityService, _httpContextKeys,
                            _logger, AdaptorUserRoleType.Submitter, job.ProjectId);
            if (job.Submitter.Id != loggedUser.Id)
                throw new AdaptorUserNotAuthorizedForJobException("UserNotAuthorizedToWorkWithJob",
                    loggedUser.GetLogIdentification(), job.Id);
            var fileTransferLogic = LogicFactory.GetLogicFactory().CreateFileTransferLogic(unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _logger);
            return fileTransferLogic.UploadFileToJobExecutionDir(fileStream, fileName, createdJobInfoId, createdTaskInfoId, loggedUser);
        }
    }
}
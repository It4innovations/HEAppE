using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExtModels.DataTransfer.Converts;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.ServiceTier.UserAndLimitationManagement;
using SshCaAPI;

namespace HEAppE.ServiceTier.DataTransfer;

public class DataTransferService : IDataTransferService
{
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    public DataTransferService(ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        
    }
    public DataTransferMethodExt RequestDataTransfer(string nodeIPAddress, int nodePort, long submittedTaskInfoId,
        string sessionCode)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
        if (submittedTaskInfo == null)
            throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);
        var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _sshCertificateAuthorityService, _httpContextKeys,
            AdaptorUserRoleType.Submitter, submittedTaskInfo.Project.Id);
        var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        var dataTransferMethod =
            dataTransferLogic.GetDataTransferMethod(nodeIPAddress, nodePort, submittedTaskInfoId, loggedUser);
        return dataTransferMethod.ConvertIntToExt();
    }

    public void CloseDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(usedTransferMethod.SubmittedTaskId);
        if (submittedTaskInfo == null)
            throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", usedTransferMethod.SubmittedTaskId);
        var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _sshCertificateAuthorityService, _httpContextKeys,
            AdaptorUserRoleType.Submitter, submittedTaskInfo.Project.Id);
        var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        dataTransferLogic.EndDataTransfer(usedTransferMethod.ConvertExtToInt(), loggedUser);
    }

    public async Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders,
        long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
        if (submittedTaskInfo == null)
            throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);
        var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _sshCertificateAuthorityService, _httpContextKeys,
            AdaptorUserRoleType.Submitter, submittedTaskInfo.Project.Id);
        var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        return await dataTransferLogic.HttpGetToJobNodeAsync(httpRequest, httpHeaders.Select(s => s.ConvertExtToInt()),
            submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser);
    }

    public async Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders,
        string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
        if (submittedTaskInfo == null)
            throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);
        var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork, _sshCertificateAuthorityService, _httpContextKeys,
            AdaptorUserRoleType.Submitter, submittedTaskInfo.Project.Id);
        var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork, _sshCertificateAuthorityService, _httpContextKeys);
        return await dataTransferLogic.HttpPostToJobNodeAsync(httpRequest, httpHeaders.Select(s => s.ConvertExtToInt()),
            httpPayload, submittedTaskInfoId, nodeIPAddress, nodePort, loggedUser);
    }
    
    public async Task HttpPostToJobNodeStreamAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders,
        string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode,
        Stream responseStream, CancellationToken cancellationToken)
    {
        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var submittedTaskInfo = unitOfWork.SubmittedTaskInfoRepository.GetById(submittedTaskInfoId);
        if (submittedTaskInfo == null)
            throw new RequestedObjectDoesNotExistException("NotExistingTaskInfo", submittedTaskInfoId);
    
        var loggedUser = UserAndLimitationManagementService.GetValidatedUserForSessionCode(sessionCode, unitOfWork,
            AdaptorUserRoleType.Submitter, submittedTaskInfo.Project.Id);
    
        var dataTransferLogic = LogicFactory.GetLogicFactory().CreateDataTransferLogic(unitOfWork);
        await dataTransferLogic.HttpPostToJobNodeStreamAsync(
            httpRequest, 
            httpHeaders.Select(s => s.ConvertExtToInt()),
            httpPayload, 
            submittedTaskInfoId, 
            nodeIPAddress, 
            nodePort, 
            loggedUser,
            responseStream,
            cancellationToken);
    }
}
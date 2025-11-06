using System.Net.Http;
using HEAppE.Authentication;
using HEAppE.BusinessLogicTier.Logic.AdminUserManagement;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;
using HEAppE.BusinessLogicTier.logic.FileTransfer;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Factory;

public class PocoLogicFactory : LogicFactory
{
    public override IAdminUserManagementLogic CreateAdminUserManagementLogic(IUnitOfWork unitOfWork)
    {
        return new AdminUserManagementLogic(unitOfWork);
    }

    public override IClusterInformationLogic CreateClusterInformationLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        return new ClusterInformationLogic(unitOfWork, sshCertificateAuthorityService, httpContextKeys);
    }

    public override IDataTransferLogic CreateDataTransferLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        return new DataTransferLogic(unitOfWork, sshCertificateAuthorityService, httpContextKeys);
    }

    public override IFileTransferLogic CreateFileTransferLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        return new FileTransferLogic(unitOfWork, sshCertificateAuthorityService, httpContextKeys);
    }

    public override IJobManagementLogic CreateJobManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        return new JobManagementLogic(unitOfWork, sshCertificateAuthorityService, httpContextKeys);
    }

    public override IJobReportingLogic CreateJobReportingLogic(IUnitOfWork unitOfWork)
    {
        return new JobReportingLogic(unitOfWork);
    }


    public override IUserAndLimitationManagementLogic CreateUserAndLimitationManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        using var scope = ServiceProvider.CreateScope();
        var httpFac = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var rtn = new UserAndLimitationManagementLogic(unitOfWork, httpFac, sshCertificateAuthorityService, httpContextKeys);

        return rtn;
    }

    public override IManagementLogic CreateManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys)
    {
        return new ManagementLogic(unitOfWork, sshCertificateAuthorityService, httpContextKeys);
    }
}
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

    public override IClusterInformationLogic CreateClusterInformationLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        return new ClusterInformationLogic(unitOfWork, sshCertificateAuthorityService);
    }

    public override IDataTransferLogic CreateDataTransferLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        return new DataTransferLogic(unitOfWork, sshCertificateAuthorityService);
    }

    public override IFileTransferLogic CreateFileTransferLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        return new FileTransferLogic(unitOfWork, sshCertificateAuthorityService);
    }

    public override IJobManagementLogic CreateJobManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        return new JobManagementLogic(unitOfWork, sshCertificateAuthorityService);
    }

    public override IJobReportingLogic CreateJobReportingLogic(IUnitOfWork unitOfWork)
    {
        return new JobReportingLogic(unitOfWork);
    }


    public override IUserAndLimitationManagementLogic CreateUserAndLimitationManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        using var scope = ServiceProvider.CreateScope();
        var httpFac = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var rtn = new UserAndLimitationManagementLogic(unitOfWork, httpFac, sshCertificateAuthorityService);

        return rtn;
    }

    public override IManagementLogic CreateManagementLogic(IUnitOfWork unitOfWork, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        return new ManagementLogic(unitOfWork, sshCertificateAuthorityService);
    }
}
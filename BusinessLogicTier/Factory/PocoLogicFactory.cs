using System.Net.Http;
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

namespace HEAppE.BusinessLogicTier.Factory;

public class PocoLogicFactory : LogicFactory
{
    public override IAdminUserManagementLogic CreateAdminUserManagementLogic(IUnitOfWork unitOfWork)
    {
        return new AdminUserManagementLogic(unitOfWork);
    }

    public override IClusterInformationLogic CreateClusterInformationLogic(IUnitOfWork unitOfWork)
    {
        return new ClusterInformationLogic(unitOfWork);
    }

    public override IDataTransferLogic CreateDataTransferLogic(IUnitOfWork unitOfWork)
    {
        return new DataTransferLogic(unitOfWork);
    }

    public override IFileTransferLogic CreateFileTransferLogic(IUnitOfWork unitOfWork)
    {
        return new FileTransferLogic(unitOfWork);
    }

    public override IJobManagementLogic CreateJobManagementLogic(IUnitOfWork unitOfWork)
    {
        return new JobManagementLogic(unitOfWork);
    }

    public override IJobReportingLogic CreateJobReportingLogic(IUnitOfWork unitOfWork)
    {
        return new JobReportingLogic(unitOfWork);
    }


    public override IUserAndLimitationManagementLogic CreateUserAndLimitationManagementLogic(IUnitOfWork unitOfWork)
    {
        using var scope = ServiceProvider.CreateScope();
        var httpFac = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
        var rtn = new UserAndLimitationManagementLogic(unitOfWork, httpFac);

        return rtn;
    }

    public override IManagementLogic CreateManagementLogic(IUnitOfWork unitOfWork)
    {
        return new ManagementLogic(unitOfWork);
    }
}
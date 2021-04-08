using HEAppE.BusinessLogicTier.Logic.AdminUserManagement;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.BusinessLogicTier.Logic.Notifications;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DataAccessTier.UnitOfWork;

namespace HEAppE.BusinessLogicTier.Factory
{
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

        public override INotificationLogic CreateNotificationLogic(IUnitOfWork unitOfWork)
        {
            return new NotificationLogic(unitOfWork);
        }

        public override IUserAndLimitationManagementLogic CreateUserAndLimitationManagementLogic(IUnitOfWork unitOfWork)
        {
            return new UserAndLimitationManagementLogic(unitOfWork);
        }
    }
}
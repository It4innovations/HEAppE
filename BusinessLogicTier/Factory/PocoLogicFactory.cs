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

      using var scope = LogicFactory.ServiceProvider.CreateScope();
      var httpFac = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
      var options = scope.ServiceProvider.GetService<IOptions<LexisAuthenticationConfiguration>>();
      return new UserAndLimitationManagementLogic(unitOfWork, options, httpFac);

    }

    public override IManagementLogic CreateManagementLogic(IUnitOfWork unitOfWork)
    {
      return new ManagementLogic(unitOfWork);

    }


  }
}
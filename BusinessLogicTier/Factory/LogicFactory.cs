using System;
using System.Collections.Generic;
using HEAppE.BusinessLogicTier.Logic.AdminUserManagement;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Factory;

public abstract class LogicFactory
{
    private static readonly Dictionary<BusinessLogicType, LogicFactory> factoryInstances =
        new(Enum.GetValues(typeof(BusinessLogicType)).Length);

    /// <summary>
    ///     Hack, initialized in Startup
    /// </summary>
    public static IServiceProvider ServiceProvider { get; set; }

    #region Instantiation

    public static LogicFactory GetLogicFactory()
    {
        return GetLogicFactory(BusinessLogicType.Poco);
    }

    public static LogicFactory GetLogicFactory(BusinessLogicType type)
    {
        lock (factoryInstances)
        {
            if (!factoryInstances.ContainsKey(type)) factoryInstances.Add(type, CreateLogicFactory(type));
        }

        return factoryInstances[type];
    }

    private static LogicFactory CreateLogicFactory(BusinessLogicType type)
    {
        switch (type)
        {
            case BusinessLogicType.Poco:
                return new PocoLogicFactory();
        }

        throw new ArgumentException(
            $"Business logic factory for type {type} is not implemented. Check the switch in HaaSMiddleware.BusinessLogicTier.Factory.AbstracLogicFactory.CreateLogicFactory(BusinessLogicType type) method.");
    }

    #endregion

    #region Abstract methods

    public abstract IAdminUserManagementLogic CreateAdminUserManagementLogic(IUnitOfWork unitOfWork);

    public abstract IClusterInformationLogic CreateClusterInformationLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService);
    public abstract IDataTransferLogic CreateDataTransferLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService);
    public abstract IFileTransferLogic CreateFileTransferLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService);
    public abstract IJobManagementLogic CreateJobManagementLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService);
    public abstract IJobReportingLogic CreateJobReportingLogic(IUnitOfWork unitOfWork);
    public abstract IUserAndLimitationManagementLogic CreateUserAndLimitationManagementLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService);

    public abstract IManagementLogic CreateManagementLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService);

    #endregion
}
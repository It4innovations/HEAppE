using System;
using System.Collections.Generic;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Logic.AdminUserManagement;
using HEAppE.BusinessLogicTier.Logic.ClusterInformation;
using HEAppE.BusinessLogicTier.Logic.DataTransfer;
using HEAppE.BusinessLogicTier.Logic.FileTransfer;
using HEAppE.BusinessLogicTier.Logic.JobManagement;
using HEAppE.BusinessLogicTier.Logic.JobReporting;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.Services.UserOrg;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.Factory;

public abstract class LogicFactory
{
    private static readonly Dictionary<BusinessLogicType, LogicFactory> factoryInstances =
        new(Enum.GetValues(typeof(BusinessLogicType)).Length);

    /// <summary>
    ///  Service provider for dependency injection
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
        ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys);
    public abstract IDataTransferLogic CreateDataTransferLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys);
    public abstract IFileTransferLogic CreateFileTransferLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys);
    public abstract IJobManagementLogic CreateJobManagementLogic(IUnitOfWork unitOfWork, IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys);
    public abstract IJobReportingLogic CreateJobReportingLogic(IUnitOfWork unitOfWork);

    public abstract IUserAndLimitationManagementLogic CreateUserAndLimitationManagementLogic(IUnitOfWork unitOfWork,
        IUserOrgService userOrgService, ISshCertificateAuthorityService sshCertificateAuthorityService,
        IHttpContextKeys httpContextKeys);

    public abstract IManagementLogic CreateManagementLogic(IUnitOfWork unitOfWork,
        ISshCertificateAuthorityService sshCertificateAuthorityService, IHttpContextKeys httpContextKeys);

    #endregion
}
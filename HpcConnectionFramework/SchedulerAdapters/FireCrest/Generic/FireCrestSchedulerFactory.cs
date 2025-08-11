using System;
using System.Collections.Generic;
using HEAppE.ConnectionPool;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.FireCrest.Generic;

/// <summary>
///     FireCrest scheduler factory for creating and managing FireCrest-related components
/// </summary>
internal class FireCrestSchedulerFactory : SchedulerFactory
{
    #region Instances

    /// <summary>
    ///     Connector instances mapped by cluster master node name
    /// </summary>
    private readonly Dictionary<string, IPoolableAdapter> _connectorSingletons = new();

    /// <summary>
    ///     Scheduler instances mapped by cluster and project details
    /// </summary>
    private readonly Dictionary<(string, long projectId, DateTime?, long?), IRexScheduler> _schedulerSingletons = new();

    /// <summary>
    ///     Data convertor singleton for translating between HEAppE and FireCrest formats
    /// </summary>
    private ISchedulerDataConvertor _convertorSingleton;

    /// <summary>
    ///     Scheduler adapter instance for interacting with FireCrest API
    /// </summary>
    private ISchedulerAdapter _schedulerAdapterInstance;

    #endregion

    #region SchedulerFactory Members

    /// <summary>
    ///     Create or get existing scheduler instance for the specified cluster and project
    /// </summary>
    /// <param name="configuration">Cluster configuration data</param>
    /// <param name="project">Project information</param>
    /// <param name="adaptorUserId">Optional adapter user ID for one-to-one mapping</param>
    /// <returns>Scheduler instance</returns>
    public override IRexScheduler CreateScheduler(Cluster configuration, Project project, long? adaptorUserId)
    {
        var uniqueIdentifier = (configuration.MasterNodeName, project.Id, project.ModifiedAt, project.IsOneToOneMapping ? adaptorUserId : null);
        
        if (!_schedulerSingletons.ContainsKey(uniqueIdentifier))
        {
            _schedulerSingletons[uniqueIdentifier] = new RexSchedulerWrapper
            (
                GetSchedulerConnectionPool(configuration, project, adaptorUserId: adaptorUserId),
                CreateSchedulerAdapter()
            );
        }
        
        return _schedulerSingletons[uniqueIdentifier];
    }

    /// <summary>
    ///     Create or get existing FireCrest scheduler adapter instance
    /// </summary>
    /// <returns>FireCrest scheduler adapter</returns>
    protected override ISchedulerAdapter CreateSchedulerAdapter()
    {
        return _schedulerAdapterInstance ??= new FireCrestSchedulerAdapter(CreateDataConvertor());
    }

    /// <summary>
    ///     Create or get existing FireCrest data convertor
    /// </summary>
    /// <returns>FireCrest data convertor</returns>
    protected override ISchedulerDataConvertor CreateDataConvertor()
    {
        return _convertorSingleton ??= new FireCrestDataConvertor(new FireCrestConversionAdapterFactory());
    }

    /// <summary>
    ///     Create or get existing SSH connector for the specified cluster
    /// </summary>
    /// <param name="configuration">Cluster configuration data</param>
    /// <returns>SSH connector instance</returns>
    protected override IPoolableAdapter CreateSchedulerConnector(Cluster configuration)
    {
        var masterNodeName = configuration.MasterNodeName;
        
        if (!_connectorSingletons.ContainsKey(masterNodeName))
        {
            _connectorSingletons[masterNodeName] = new SshConnector();
        }

        return _connectorSingletons[masterNodeName];
    }

    #endregion
}

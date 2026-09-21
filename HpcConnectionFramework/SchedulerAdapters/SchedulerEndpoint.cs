using System;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters;

/// <summary>
///     Scheduler endpoint
/// </summary>
internal struct SchedulerEndpoint
{
    #region Properties

    /// <summary>
    ///     Master nodename (host)
    /// </summary>
    public string MasterNodeName { get; }

    /// <summary>
    ///     Project ID
    /// </summary>
    public long ProjectId { get; }

    /// <summary>
    ///     Last time when project was updated
    /// </summary>
    public DateTime? ProjectModifiedAt { get; set; }

    /// <summary>
    ///     Scheduler type
    /// </summary>
    public SchedulerType SchedulerType { get; }

    /// <summary>
    ///     Adaptor ID
    /// </summary>
    public long? AdaptorUserId { get; }

    /// <summary>
    ///     Used for FirecREST proxy.
    /// </summary>
    public long? ProxyConnectionId { get; }

    /// <summary>
    ///     Connection protocol (SSH, HTTP, HTTPS)
    /// </summary>
    public ClusterConnectionProtocol ConnectionProtocol { get; }

    /// <summary>
    ///     Connection port
    /// </summary>
    public int? Port { get; }

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public SchedulerEndpoint(string masterNodeName, long projectId, DateTime? projectModifiedAt,
        SchedulerType schedulerType, long? adaptorUserId, long? proxyConnectionId,
        ClusterConnectionProtocol connectionProtocol, int? port)
    {
        MasterNodeName = masterNodeName;
        SchedulerType = schedulerType;
        ProjectModifiedAt = projectModifiedAt;
        ProjectId = projectId;
        AdaptorUserId = adaptorUserId;
        ProxyConnectionId = proxyConnectionId;
        ConnectionProtocol = connectionProtocol;
        Port = port;
    }

    #endregion

    #region Override Methods

    /// <summary>
    ///     Equals
    /// </summary>
    public override bool Equals(object obj)
    {
        return obj is SchedulerEndpoint endpoint &&
               MasterNodeName.Equals(endpoint.MasterNodeName) &&
               ProjectId.Equals(endpoint.ProjectId) &&
               SchedulerType.Equals(endpoint.SchedulerType) &&
               Nullable.Equals(AdaptorUserId, endpoint.AdaptorUserId) &&
               Nullable.Equals(ProxyConnectionId, endpoint.ProxyConnectionId) &&
               ConnectionProtocol.Equals(endpoint.ConnectionProtocol) &&
               Nullable.Equals(Port, endpoint.Port);
    }

    /// <summary>
    ///     Get hash code
    /// </summary>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(MasterNodeName);
        hash.Add(ProjectId);
        hash.Add(SchedulerType);
        hash.Add(AdaptorUserId);
        hash.Add(ProxyConnectionId);
        hash.Add(ConnectionProtocol);
        hash.Add(Port);
        return hash.ToHashCode();
    }

    #endregion
}
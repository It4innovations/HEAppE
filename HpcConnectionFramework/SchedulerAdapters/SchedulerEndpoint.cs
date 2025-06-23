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

    #endregion

    #region Constructors,

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="masterNodeName">Master node name</param>
    /// <param name="projectId">Project ID</param>
    /// <param name="projectModifiedAt"></param>
    /// <param name="schedulerType">Scheduler type</param>
    public SchedulerEndpoint(string masterNodeName, long projectId, DateTime? projectModifiedAt,
        SchedulerType schedulerType, long? adaptorUserId)
    {
        MasterNodeName = masterNodeName;
        SchedulerType = schedulerType;
        ProjectModifiedAt = projectModifiedAt;
        ProjectId = projectId;
        AdaptorUserId = adaptorUserId;
    }

    #endregion

    #region Override Methods

    /// <summary>
    ///     Equals
    /// </summary>
    /// <param name="obj">Object</param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        return obj is SchedulerEndpoint endpoint &&
               MasterNodeName.Equals(endpoint.MasterNodeName) &&
               ProjectId.Equals(endpoint.ProjectId) &&
               ProjectModifiedAt.Equals(endpoint.ProjectModifiedAt) &&
               SchedulerType.Equals(endpoint.SchedulerType) &&
               AdaptorUserId.Equals(endpoint.AdaptorUserId);
    }

    /// <summary>
    ///     Get hash code
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(MasterNodeName, ProjectId, ProjectModifiedAt, SchedulerType, AdaptorUserId);
    }

    #endregion
}
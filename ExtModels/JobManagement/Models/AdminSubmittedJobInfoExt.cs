using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.ExtModels.ClusterInformation.Models;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Detailed submitted job info for admin report
/// </summary>
[DataContract(Name = "AdminSubmittedJobInfoExt")]
[Description("Detailed submitted job info for admin report")]
public class AdminSubmittedJobInfoExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// State
    /// </summary>
    [DataMember(Name = "State")]
    [Description("State")]
    public JobStateExt? State { get; set; }

    /// <summary>
    /// Source of state update
    /// </summary>
    [DataMember(Name = "StateSource")]
    [Description("Source of state update")]
    public JobStateSourceExt? StateSource { get; set; }

    /// <summary>
    /// Timestamp of last state update
    /// </summary>
    [DataMember(Name = "StateUpdatedAt")]
    [Description("Timestamp of last state update")]
    public DateTime? StateUpdatedAt { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    [DataMember(Name = "CreationTime")]
    [Description("Creation time")]
    public DateTime? CreationTime { get; set; }

    /// <summary>
    /// Submit time
    /// </summary>
    [DataMember(Name = "SubmitTime")]
    [Description("Submit time")]
    public DateTime? SubmitTime { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    [DataMember(Name = "StartTime")]
    [Description("Start time")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [DataMember(Name = "EndTime")]
    [Description("End time")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total allocation time
    /// </summary>
    [DataMember(Name = "TotalAllocatedTime")]
    [Description("Total allocation time")]
    public double? TotalAllocatedTime { get; set; }

    /// <summary>
    /// Submitter details (AdaptorUser identity: Id, Username, Email, IdpSid)
    /// </summary>
    [DataMember(Name = "Submitter")]
    [Description("Submitter details (AdaptorUser identity: Id, Username, Email, IdpSid)")]
    public AdminSubmitterInfoExt Submitter { get; set; }

    /// <summary>
    /// Submitter group name
    /// </summary>
    [DataMember(Name = "SubmitterGroup")]
    [Description("Submitter group name")]
    public string SubmitterGroup { get; set; }

    /// <summary>
    /// Cluster username used internally on HPC cluster
    /// </summary>
    [DataMember(Name = "ClusterUsername")]
    [Description("Cluster username used internally on HPC cluster")]
    public string ClusterUsername { get; set; }

    /// <summary>
    /// Cluster authentication type
    /// </summary>
    [DataMember(Name = "ClusterAuthenticationType")]
    [Description("Cluster authentication type")]
    public ClusterAuthenticationCredentialsAuthTypeExt? ClusterAuthenticationType { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project ID")]
    public long? ProjectId { get; set; }

    /// <summary>
    /// Project name
    /// </summary>
    [DataMember(Name = "ProjectName")]
    [Description("Project name")]
    public string ProjectName { get; set; }

    /// <summary>
    /// SubProject ID
    /// </summary>
    [DataMember(Name = "SubProjectId")]
    [Description("SubProject ID")]
    public long? SubProjectId { get; set; }

    /// <summary>
    /// SubProject identifier
    /// </summary>
    [DataMember(Name = "SubProjectIdentifier")]
    [Description("SubProject identifier")]
    public string SubProjectIdentifier { get; set; }

    /// <summary>
    /// Cluster ID
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster ID")]
    public long? ClusterId { get; set; }

    /// <summary>
    /// Cluster name
    /// </summary>
    [DataMember(Name = "ClusterName")]
    [Description("Cluster name")]
    public string ClusterName { get; set; }

    /// <summary>
    /// HPC reservation
    /// </summary>
    [DataMember(Name = "Reservation")]
    [Description("HPC reservation string")]
    public string Reservation { get; set; }

    /// <summary>
    /// Array of tasks
    /// </summary>
    [DataMember(Name = "Tasks")]
    [Description("Array of tasks")]
    public AdminTaskInfoExt[] Tasks { get; set; }

    public override string ToString()
    {
        return $"AdminSubmittedJobInfoExt(id={Id}; name={Name}; state={State}; submitter={Submitter?.Username}; clusterUsername={ClusterUsername}; projectName={ProjectName}; tasksCount={Tasks?.Length})";
    }
}

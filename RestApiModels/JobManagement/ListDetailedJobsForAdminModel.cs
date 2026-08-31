using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for retrieving detailed list of jobs for admin
/// </summary>
[DataContract(Name = "ListDetailedJobsForAdminModel")]
[Description("Model for retrieving detailed list of jobs for admin")]
public class ListDetailedJobsForAdminModel : SessionCodeModel
{
    /// <summary>
    /// Job states (comma separated string of enum ints)
    /// </summary>
    [DataMember(Name = "JobStates")]
    [Description("Job states (comma separated string of enum ints)")]
    public string JobStates { get; set; }

    /// <summary>
    /// Max number of jobs to return
    /// </summary>
    [DataMember(Name = "Limit")]
    [Description("Max number of jobs to return")]
    public int? Limit { get; set; }

    /// <summary>
    /// Number of jobs to skip
    /// </summary>
    [DataMember(Name = "Offset")]
    [Description("Number of jobs to skip")]
    public int? Offset { get; set; }

    /// <summary>
    /// Filter by user ID
    /// </summary>
    [DataMember(Name = "UserId")]
    [Description("Filter by user ID")]
    public long? UserId { get; set; }

    /// <summary>
    /// Filter by cluster ID
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Filter by cluster ID")]
    public long? ClusterId { get; set; }

    /// <summary>
    /// Filter by subproject ID
    /// </summary>
    [DataMember(Name = "SubProjectId")]
    [Description("Filter by subproject ID")]
    public long? SubProjectId { get; set; }

    /// <summary>
    /// Filter by project ID
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Filter by project ID")]
    public long? ProjectId { get; set; }

    /// <summary>
    /// Search term (job or task name)
    /// </summary>
    [DataMember(Name = "Search")]
    [Description("Search term (job or task name)")]
    public string Search { get; set; }

    public override string ToString()
    {
        return $"ListDetailedJobsForAdminModel({base.ToString()}; jobStates={JobStates}; limit={Limit}; offset={Offset}; userId={UserId}; clusterId={ClusterId}; subProjectId={SubProjectId}; projectId={ProjectId}; search={Search})";
    }
}

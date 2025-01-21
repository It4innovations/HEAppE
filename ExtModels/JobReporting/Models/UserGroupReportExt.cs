using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// User group report ext
/// </summary>
[DataContract(Name = "UserGroupReportExt")]
[Description("User group report ext")]
public class UserGroupReportExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Total usage
    /// </summary>
    [DataMember]
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

    /// <summary>
    /// Usage type
    /// </summary>
    [DataMember]
    [Description("Usage type")]
    public UsageTypeExt UsageType { get; set; }

    /// <summary>
    /// Project
    /// </summary>
    [DataMember]
    [Description("Project")]
    public ProjectReportExt Project { get; set; }
}
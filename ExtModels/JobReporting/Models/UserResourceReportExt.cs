using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// User resource report ext
/// </summary>
[DataContract(Name = "UserResourceReportExt")]
[Description("User resource report ext")]
public class UserResourceReportExt
{
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
    /// List of project reports
    /// </summary>
    [DataMember]
    [Description("List of project reports")]
    public IEnumerable<ProjectReportExt> Projects { get; set; }
}
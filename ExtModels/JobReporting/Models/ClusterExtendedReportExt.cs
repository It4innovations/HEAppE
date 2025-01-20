using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Cluster extended report ext
/// </summary>
[DataContract(Name = "ClusterExtendedReportExt")]
[Description("Cluster extended report ext")]
public class ClusterExtendedReportExt
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
    [Required]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Total usage
    /// </summary>
    [DataMember]
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

    /// <summary>
    /// List of cluster node extended type reports
    /// </summary>
    [DataMember]
    [Description("List of cluster node extended type reports")]
    public List<ClusterNodeExtendedTypeReportExt> ClusterNodeTypes { get; set; }
}
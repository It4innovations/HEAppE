using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Cluster report ext
/// </summary>
[DataContract(Name = "ClusterReportExt")]
[Description("Cluster report ext")]
public class ClusterReportExt
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
    /// Total usage
    /// </summary>
    [DataMember]
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

    /// <summary>
    /// List of cluster node type reports
    /// </summary>
    [DataMember]
    [Description("List of cluster node type reports")]
    public List<ClusterNodeTypeReportExt> ClusterNodeTypes { get; set; }
}
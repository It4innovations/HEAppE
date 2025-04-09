using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Cluster node type report ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeReportExt")]
[Description("Cluster node type report ext")]
public class ClusterNodeTypeReportExt
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
    /// List of job reports
    /// </summary>
    [DataMember]
    [Description("List of job reports")]
    public List<JobReportExt> Jobs { get; set; }
}
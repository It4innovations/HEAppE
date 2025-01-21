using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Cluster node extended type report ext
/// </summary>
[DataContract(Name = "ClusterNodeExtendedTypeReportExt")]
[Description("Cluster node extended type report ext")]
public class ClusterNodeExtendedTypeReportExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Total usage
    /// </summary>
    [DataMember]
    [Description("Total usage")]
    public double? TotalUsage { get; set; }

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
    /// List of job extended reports
    /// </summary>
    [DataMember]
    [Description("List of job extended reports")]
    public List<JobExtendedReportExt> Jobs { get; set; }
}
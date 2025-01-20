using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport;

/// <summary>
/// Cluster node type detailed report ext
/// </summary>
[DataContract(Name = "ClusterNodeTypeDetailedReportExt")]
[Description("Cluster node type detailed report ext")]
public class ClusterNodeTypeDetailedReportExt
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
    /// Queue name
    /// </summary>
    [DataMember]
    [Description("Queue name")]
    public string QueueName { get; set; }

    /// <summary>
    /// List of job detailed reports
    /// </summary>
    [DataMember]
    [Description("List of job detailed reports")]
    public List<JobDetailedReportExt> Jobs { get; set; }
}
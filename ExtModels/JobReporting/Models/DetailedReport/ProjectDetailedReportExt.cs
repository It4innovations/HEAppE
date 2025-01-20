using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport;

/// <summary>
/// Project detailed report ext
/// </summary>
[DataContract(Name = "ProjectDetailedReportExt")]
[Description("Project detailed report ext")]
public class ProjectDetailedReportExt
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
    /// Accounting string
    /// </summary>
    [DataMember]
    [Description("Accounting string")]
    public string AccountingString { get; set; }

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
    /// Start date
    /// </summary>
    [DataMember]
    [Description("Start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [DataMember]
    [Description("End date")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// List of cluster detailed reports
    /// </summary>
    [DataMember]
    [Description("List of cluster detailed reports")]
    public List<ClusterDetailedReportExt> Clusters { get; set; }
}
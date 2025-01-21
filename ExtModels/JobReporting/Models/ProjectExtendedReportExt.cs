using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Project extended report ext
/// </summary>
[DataContract(Name = "ProjectExtendedReportExt")]
[Description("Project extended report ext")]
public class ProjectExtendedReportExt
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
    /// List of cluster extended reports
    /// </summary>
    [DataMember]
    [Description("List of cluster extended reports")]
    public List<ClusterExtendedReportExt> Clusters { get; set; }
}
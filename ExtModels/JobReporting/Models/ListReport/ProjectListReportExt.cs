using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.ListReport;

/// <summary>
/// Project list report ext
/// </summary>
[DataContract(Name = "ProjectReportExt")]
[Description("Project list report ext")]
public class ProjectListReportExt
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
}
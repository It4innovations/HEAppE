using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.ListReport;

[DataContract(Name = "ProjectReportExt")]
public class ProjectListReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public string Description { get; set; }

    [DataMember] public string AccountingString { get; set; }

    [DataMember] public double? TotalUsage { get; set; }
}
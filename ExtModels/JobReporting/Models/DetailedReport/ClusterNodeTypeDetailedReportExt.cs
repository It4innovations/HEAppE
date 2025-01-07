using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models.DetailedReport;

[DataContract(Name = "ClusterNodeTypeDetailedReportExt")]
public class ClusterNodeTypeDetailedReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public double? TotalUsage { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public string Description { get; set; }

    [DataMember] public string QueueName { get; set; }

    [DataMember] public List<JobDetailedReportExt> Jobs { get; set; }
}
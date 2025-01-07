using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

[DataContract(Name = "SubProjectReportExt")]
public class SubProjectAggregatedReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public string Name { get; set; }

    public double? TotalUsage { get; set; }

    [DataMember] public List<ClusterAggregatedReportExt> Clusters { get; set; }
}
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

[DataContract(Name = "ClusterNodeTypeReportExt")]
public class ClusterNodeTypeAggregatedReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public string Name { get; set; }

    public double? TotalUsage { get; set; }

    [DataMember] public List<ClusterNodeTypeReportExt> ClusterNodeTypes { get; set; }
}
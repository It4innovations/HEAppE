using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobReporting;

public class ClusterAggregatedReport
{
    public Cluster Cluster { get; set; }
    public List<ClusterNodeTypeAggregatedReport> ClusterNodeTypesAggregations { get; set; }
    public double? TotalUsage => Math.Round(ClusterNodeTypesAggregations.Sum(x => x.TotalUsage) ?? 0, 3);
}
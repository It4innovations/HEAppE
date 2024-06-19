using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.JobReporting;

public class ClusterNodeTypeAggregatedReport
{
    public ClusterNodeTypeAggregation ClusterNodeTypeAggregation { get; set; }
    public List<ClusterNodeTypeReport> ClusterNodeTypes { get; set; }
    public double? TotalUsage => Math.Round(ClusterNodeTypes.Sum(x => x.TotalUsage) ?? 0, 3);
}
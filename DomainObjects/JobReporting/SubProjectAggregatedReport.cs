using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.JobReporting;

public class SubProjectAggregatedReport
{
    public SubProject SubProject { get; set; }
    public List<ClusterAggregatedReport> Clusters { get; set; }
    public double? TotalUsage => Math.Round(Clusters.Sum(x => x.TotalUsage) ?? 0, 3);
}
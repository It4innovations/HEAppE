using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DomainObjects.JobReporting
{
    public class ProjectAggregatedReport
    {
        public Project Project { get; set; }
        public List<SubProjectAggregatedReport> SubProjects { get; set; }
        public List<ClusterAggregatedReport> Clusters { get; set; }
        public double? TotalUsage => Math.Round(Clusters.Sum(x => x.TotalUsage) ?? 0, 3);
    }
}
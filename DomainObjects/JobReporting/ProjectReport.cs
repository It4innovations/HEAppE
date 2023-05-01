using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.DomainObjects.JobReporting
{
    public class ProjectReport
    {
        public Project Project { get; set; }
        public List<ClusterReport> Clusters { get; set; }
        public double? TotalUsage => Clusters.Sum(x => x.TotalUsage);
    }
}
using HEAppE.DomainObjects.ClusterInformation;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.DomainObjects.JobReporting
{
    public class ClusterNodeTypeReport
    {
        public ClusterNodeType ClusterNodeType { get; set; }
        public List<JobReport> Jobs { get; set; }
        public double? TotalUsage => Jobs.Sum(x => x.Usage);
    }
}
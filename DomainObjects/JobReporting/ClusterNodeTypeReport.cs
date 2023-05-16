using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.DomainObjects.JobReporting
{
    public class ClusterNodeTypeReport
    {
        public ClusterNodeType ClusterNodeType { get; set; }
        public List<JobReport> Jobs { get; set; }
        public double? TotalUsage => Math.Round(Jobs.Sum(x => x.Usage) ?? 0, 3);
    }
}
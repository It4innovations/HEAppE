using HEAppE.ExtModels.ClusterInformation.Models;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "NodeTypeAggregatedUsageExt")]
    public class NodeTypeAggregatedUsageExt
    {
        [DataMember(Name = "ClusterNodeType")]
        public ClusterNodeTypeExt ClusterNodeType { get; set; }

        [DataMember(Name = "SubmittedTasks")]
        public IEnumerable<SubmittedTaskInfoUsageReportExtendedExt> SubmittedTasks { get; set; }

        [DataMember(Name = "TotalCorehoursUsage")]
        public double? TotalCorehoursUsage { get; set; }

        public override string ToString()
        {
            return $"NodeTypeAggregatedUsageExt(clusterNodeType={ClusterNodeType}; submittedTasks={SubmittedTasks}; totalCorehoursUsage={TotalCorehoursUsage})";
        }
    }
}

using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ClusterNodeUsageExt")]
    public class ClusterNodeUsageExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Priority")]
        public int? Priority { get; set; }

        [DataMember(Name = "CoresPerNode")]
        public int? CoresPerNode { get; set; }

        [DataMember(Name = "MaxWalltime")]
        public int? MaxWalltime { get; set; }

        [DataMember(Name = "NumberOfNodes")]
        public int? NumberOfNodes { get; set; }

        [DataMember(Name = "NumberOfUsedNodes")]
        public int? NumberOfUsedNodes { get; set; }

        [DataMember(Name = "TotalJobs")]
        public int? TotalJobs { get; set; }

        public override string ToString()
        {
            return $"ClusterNodeUsageExt(id={Id}; name={Name}; description={Description}; priority={Priority}; coresPerNode={CoresPerNode}; maxWallTime={MaxWalltime}; numberOfNodes={NumberOfNodes}; numberOfUsedNodes={NumberOfUsedNodes}; totalJobs={TotalJobs})";
        }
    }
}

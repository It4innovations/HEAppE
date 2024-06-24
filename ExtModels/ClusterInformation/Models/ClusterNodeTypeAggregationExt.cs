using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ClusterNodeTypeAggregationExt")]
    public class ClusterNodeTypeAggregationExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "AllocationType")]
        public string AllocationType { get; set; }

        [DataMember(Name = "ValidityFrom")]
        public DateTime ValidityFrom { get; set; }

        [DataMember(Name = "ValidityTo")]
        public DateTime? ValidityTo { get; set; }

        public override string ToString()
        {
            return $"ClusterNodeTypeAggregationExt(id={Id}; name={Name}; description={Description}; allocationType={AllocationType}; validityFrom={ValidityFrom}; validityTo={ValidityTo})";
        }
    }
}

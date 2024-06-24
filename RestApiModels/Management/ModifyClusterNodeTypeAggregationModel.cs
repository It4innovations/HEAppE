using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ModifyClusterNodeTypeAggregationModel")]
    public class ModifyClusterNodeTypeAggregationModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }

        [DataMember(Name = "Name", IsRequired = true), StringLength(50)]
        public string Name { get; set; }

        [DataMember(Name = "Description"), StringLength(100)]
        public string Description { get; set; }

        [DataMember(Name = "AllocationType")]
        public string AllocationType { get; set; }

        [DataMember(Name = "ValidityFrom", IsRequired = true)]
        public DateTime ValidityFrom { get; set; }

        [DataMember(Name = "ValidityTo")]
        public DateTime? ValidityTo { get; set; }
    }
}

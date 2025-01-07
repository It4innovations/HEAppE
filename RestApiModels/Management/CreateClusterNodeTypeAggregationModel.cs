using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateClusterNodeTypeAggregationModel")]
public class CreateClusterNodeTypeAggregationModel : SessionCodeModel
{
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(50)]
    public string Name { get; set; }

    [DataMember(Name = "Description")]
    [StringLength(100)]
    public string Description { get; set; }

    [DataMember(Name = "AllocationType")] public string AllocationType { get; set; }

    [DataMember(Name = "ValidityFrom", IsRequired = true)]
    public DateTime ValidityFrom { get; set; }

    [DataMember(Name = "ValidityTo")] public DateTime? ValidityTo { get; set; }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateSubProjectModel")]
public class CreateSubProjectModel : SessionCodeModel
{
    [DataMember(Name = "Identifier", IsRequired = true), StringLength(50)]
    public string Identifier { get; set; }
    [DataMember(Name = "Description", IsRequired = false), StringLength(100)]
    public string Description { get; set; }
    [DataMember(Name = "StartDate", IsRequired = true)]
    public DateTime StartDate { get; set; }
    [DataMember(Name = "EndDate", IsRequired = false)]
    public DateTime? EndDate { get; set; }
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"CreateSubProjectModel({base.ToString()}; Identifier: {Identifier}, Description: {Description}, StartDate: {StartDate}, EndDate: {EndDate}, ProjectId: {ProjectId})";
    }
}
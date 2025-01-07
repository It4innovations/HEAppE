using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifySubProjectModel")]
public class ModifySubProjectModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long Id { get; set; }

    [DataMember(Name = "Identifier", IsRequired = true)]
    [StringLength(50)]
    public string Identifier { get; set; }

    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    public string Description { get; set; }

    [DataMember(Name = "StartDate", IsRequired = true)]
    public DateTime StartDate { get; set; }

    [DataMember(Name = "EndDate", IsRequired = false)]
    public DateTime? EndDate { get; set; }

    public override string ToString()
    {
        return
            $"ModifySubProjectModel({base.ToString()}; Id: {Id}, Identifier: {Identifier}, Description: {Description}, StartDate: {StartDate}, EndDate: {EndDate})";
    }
}
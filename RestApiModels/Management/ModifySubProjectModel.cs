using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify sub project model
/// </summary>
[DataContract(Name = "ModifySubProjectModel")]
[Description("Modify sub project model")]
public class ModifySubProjectModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Identifier
    /// </summary>
    [DataMember(Name = "Identifier", IsRequired = true)]
    [StringLength(50)]
    [Description("Identifier")]
    public string Identifier { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    [DataMember(Name = "StartDate", IsRequired = true)]
    [Description("Start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [DataMember(Name = "EndDate", IsRequired = false)]
    [Description("End date")]
    public DateTime? EndDate { get; set; }

    public override string ToString()
    {
        return $"ModifySubProjectModel({base.ToString()}; Id: {Id}, Identifier: {Identifier}, Description: {Description}, StartDate: {StartDate}, EndDate: {EndDate})";
    }
}
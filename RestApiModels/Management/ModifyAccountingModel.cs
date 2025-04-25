using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify accounting model
/// </summary>
[DataContract(Name = "ModifyAccountingModel")]
[Description("Modify accounting model")]
public class ModifyAccountingModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Formula
    /// </summary>
    [DataMember(Name = "Formula", IsRequired = true)]
    [StringLength(200)]
    [Description("Formula")]
    public string Formula { get; set; }

    /// <summary>
    /// Validity from
    /// </summary>
    [DataMember(Name = "ValidityFrom", IsRequired = true)]
    [Description("Validity from")]
    public DateTime ValidityFrom { get; set; }

    /// <summary>
    /// Validity to
    /// </summary>
    [DataMember(Name = "ValidityTo")]
    [Description("Validity to")]
    public DateTime? ValidityTo { get; set; }
}
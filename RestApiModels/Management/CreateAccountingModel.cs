using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create accounting model
/// </summary>
[DataContract(Name = "CreateAccountingModel")]
[Description("Create accounting model")]
public class CreateAccountingModel : SessionCodeModel
{
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
    [DataMember(Name = "ValidityTo", IsRequired = false)]
    [Description("Validity to")]
    public DateTime? ValidityTo { get; set; }
}
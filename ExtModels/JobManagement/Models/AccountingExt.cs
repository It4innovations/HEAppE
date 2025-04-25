using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Accounting ext
/// </summary>
[DataContract(Name = "AccountingExt")]
[Description("Accounting ext")]
public class AccountingExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Formula
    /// </summary>
    [DataMember(Name = "Formula")]
    [Description("Formula")]
    public string Formula { get; set; }

    /// <summary>
    /// Created at date
    /// </summary>
    [DataMember(Name = "CreatedAt")]
    [Description("Created at date")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modified at date
    /// </summary>
    [DataMember(Name = "ModifiedAt")]
    [Description("Modified at date")]
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Validity from date
    /// </summary>
    [DataMember(Name = "ValidityFrom")]
    [Description("Validity from date")]
    public DateTime ValidityFrom { get; set; }

    /// <summary>
    /// Validity to date
    /// </summary>
    [DataMember(Name = "ValidityTo")]
    [Description("Validity to date")]
    public DateTime? ValidityTo { get; set; }

    public override string ToString()
    {
        return $"AccountingExt(Id={Id}; Formula={Formula}; CreatedAt={CreatedAt}; ModifiedAt={ModifiedAt}; ValidityFrom={ValidityFrom}; ValidityTo={ValidityTo})";
    }
}
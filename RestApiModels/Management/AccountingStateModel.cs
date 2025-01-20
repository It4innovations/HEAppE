using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Accounting state model
/// </summary>
[DataContract(Name = "AccountingStateModel")]
[Description("Accounting state model")]
public class AccountingStateModel : SessionCodeModel
{
    /// <summary>
    /// Project id
    /// </summary>
    [Description("Project id")]
    public long ProjectId { get; set; }
}
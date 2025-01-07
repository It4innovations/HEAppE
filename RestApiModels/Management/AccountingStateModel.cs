using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "AccountingStateModel")]
public class AccountingStateModel : SessionCodeModel
{
    public long ProjectId { get; set; }
}
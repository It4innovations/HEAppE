using System;
using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "AccountingStateModel")]
    public class AccountingStateModel : SessionCodeModel
    {
        public long ProjectId { get; set; }
    }
}

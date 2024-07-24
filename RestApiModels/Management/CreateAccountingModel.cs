using HEAppE.RestApiModels.AbstractModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateAccountingModel")]
    public class CreateAccountingModel : SessionCodeModel
    {
        [DataMember(Name = "Formula", IsRequired = true), StringLength(200)]
        public string Formula { get; set; }

        [DataMember(Name = "ValidityFrom", IsRequired = true)]
        public DateTime ValidityFrom { get; set; }
    }
}

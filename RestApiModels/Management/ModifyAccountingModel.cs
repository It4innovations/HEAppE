using System.ComponentModel.DataAnnotations;
using System;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ModifyAccountingModel")]
    public class ModifyAccountingModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }

        [DataMember(Name = "Formula", IsRequired = true), StringLength(200)]
        public string Formula { get; set; }

        [DataMember(Name = "ValidityFrom", IsRequired = true)]
        public DateTime ValidityFrom { get; set; }

        [DataMember(Name = "ValidityTo")]
        public DateTime? ValidityTo { get; set; }
    }
}

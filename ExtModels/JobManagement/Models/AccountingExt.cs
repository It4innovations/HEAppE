using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "AccountingExt")]
    public class AccountingExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Formula")]
        public string Formula { get; set; }

        [DataMember(Name = "CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [DataMember(Name = "ModifiedAt")]
        public DateTime? ModifiedAt { get; set; }

        [DataMember(Name = "ValidityFrom")]
        public DateTime ValidityFrom { get; set; }

        [DataMember(Name = "ValidityTo")]
        public DateTime? ValidityTo { get; set; }

        public override string ToString()
        {
            return $"AccountingExt(Id={Id}; Formula={Formula}; CreatedAt={CreatedAt}; ModifiedAt={ModifiedAt}; ValidityFrom={ValidityFrom}; ValidityTo={ValidityTo})";
        }
    }
}

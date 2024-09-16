using System;

namespace HEAppE.ExtModels.Management.Models
{
    public class AccountingStateExt
    {
        public long ProjectId { get; set; }
        public AccountingStateTypeExt State { get; set; }
        public DateTime ComputingStartDate { get; set; }
        public DateTime? ComputingEndDate { get; set; }
        public DateTime TriggeredAt { get; set; }
        public DateTime? LastUpdatedAt { get; set; }
    }
}
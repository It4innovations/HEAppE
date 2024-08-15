using System;
using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ComputeAccountingModel")]
    public class ComputeAccountingModel : SessionCodeModel
    {
        [DataMember(Name = "StartTime", IsRequired = true)]
        public DateTime StartTime { get; set; }
        [DataMember(Name = "EndTime", IsRequired = true)]
        public DateTime EndTime { get; set; }
        
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
    }
}

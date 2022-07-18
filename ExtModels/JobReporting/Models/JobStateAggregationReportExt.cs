using HEAppE.ExtModels.JobManagement.Models;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "JobStateAggregationReportExt")]
    public class JobStateAggregationReportExt
    {
        [DataMember(Name = "JobStateId")]
        public JobStateExt JobStateId { get; set; }

        [DataMember(Name = "JobStateName")]
        public string JobStateName { get; set; }

        [DataMember(Name = "Count")]
        public long Count { get; set; }

        public override string ToString()
        {
            return $"JobStateAggregationReportExt: State={JobStateId}, StateName={JobStateName}, Count={Count}";
        }
    }
}

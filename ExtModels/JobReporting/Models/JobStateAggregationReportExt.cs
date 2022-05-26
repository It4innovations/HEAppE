using HEAppE.ExtModels.JobManagement.Models;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models
{
    [DataContract(Name = "JobStateAggregationReportExt")]
    public class JobStateAggregationReportExt
    {
        [DataMember(Name = "State")]
        public JobStateExt State { get; set; }

        [DataMember(Name = "Count")]
        public long Count { get; set; }

        public override string ToString()
        {
            return $"JobStateAggregationReportExt: State={State}, Count={Count}";
        }
    }
}

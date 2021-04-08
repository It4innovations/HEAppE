using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobReporting
{
    [DataContract(Name = "GetResourceUsageReportForJobModel")]
    public class GetResourceUsageReportForJobModel
    {
        [DataMember(Name = "JobId")]
        public long JobId { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobReporting
{
    [DataContract(Name = "GetUserResourceUsageReportModel")]
    public class GetUserResourceUsageReportModel
    {
        [DataMember(Name = "UserId")]
        public long UserId { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime EndTime { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

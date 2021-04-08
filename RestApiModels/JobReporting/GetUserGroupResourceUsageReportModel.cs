using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobReporting
{
    [DataContract(Name = "GetUserGroupResourceUsageReportModel")]
    public class GetUserGroupResourceUsageReportModel
    {
        [DataMember(Name = "GroupId")]
        public long GroupId { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime EndTime { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

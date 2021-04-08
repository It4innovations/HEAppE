using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "ReadDataFromJobNodeModel")]
    public class ReadDataFromJobNodeModel
    {
        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "IpAddress")]
        public string IpAddress { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

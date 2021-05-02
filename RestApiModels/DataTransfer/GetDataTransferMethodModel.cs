using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "GetDataTransferMethodModel")]
    public class GetDataTransferMethodModel
    {
        [DataMember(Name = "IpAddress"), StringLength(50)]
        public string IpAddress { get; set; }

        [DataMember(Name = "Port")]
        public int Port { get; set; }

        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "SessionCode"), StringLength(50)]
        public string SessionCode { get; set; }
    }
}

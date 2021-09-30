using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "HttpGetToJobNodeModel")]
    public class HttpGetToJobNodeModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "HttpRequest")]
        public string HttpRequest { get; set; }

        [DataMember(Name = "HttpHeaders")]
        public string[] HttpHeaders { get; set; }

        [DataMember(Name = "IpAddress"), StringLength(50)]
        public string IpAddress { get; set; }

    }
}

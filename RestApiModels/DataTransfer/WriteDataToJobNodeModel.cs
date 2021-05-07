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
    [DataContract(Name = "WriteDataToJobNodeModel")]
    public class WriteDataToJobNodeModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "Data")]
        public byte[] Data { get; set; }

        [DataMember(Name = "IpAddress"), StringLength(45)]
        public string IpAddress { get; set; }

        [DataMember(Name = "CloseConnection")]
        public bool CloseConnection { get; set; }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "WriteDataToJobNodeModel")]
    public class WriteDataToJobNodeModel
    {
        [DataMember(Name = "Data")]
        public byte[] Data { get; set; }

        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "IpAddress")]
        public string IpAddress { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }

        [DataMember(Name = "CloseConnection")]
        public bool CloseConnection { get; set; }
    }
}

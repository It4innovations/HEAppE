﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CopyJobDataToTempModel")]
    public class CopyJobDataToTempModel
    {
        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }

        [DataMember(Name = "Path")]
        public string Path { get; set; }
    }
}

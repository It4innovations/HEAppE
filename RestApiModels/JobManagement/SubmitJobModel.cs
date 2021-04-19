﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "SubmitJobModel")]
    public class SubmitJobModel
    {
        [DataMember(Name = "CreatedJobInfoId")]
        public long CreatedJobInfoId { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}
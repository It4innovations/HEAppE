﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "ListJobsForCurrentUserModel")]
    public class ListJobsForCurrentUserModel
    {
        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

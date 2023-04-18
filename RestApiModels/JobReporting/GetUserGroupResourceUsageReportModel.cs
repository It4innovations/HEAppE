﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting
{
    [DataContract(Name = "UserGroupResourceUsageReportModel")]
    public class UserGroupResourceUsageReportModel : SessionCodeModel
    {
        [DataMember(Name = "GroupId")]
        public long GroupId { get; set; }

        [DataMember(Name = "StartTime")]
        public DateTime StartTime { get; set; }

        [DataMember(Name = "EndTime")]
        public DateTime EndTime { get; set; }
        public override string ToString()
        {
            return $"UserGroupResourceUsageReportModel({base.ToString()}; GroupId: {GroupId}; StartTime: {StartTime}; EndTime: {EndTime})";
        }
    }
}

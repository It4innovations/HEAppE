using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting
{
    [DataContract(Name = "ResourceUsageReportForJobModel")]
    public class ResourceUsageReportForJobModel : SessionCodeModel
    {
        [DataMember(Name = "JobId")]
        public long JobId { get; set; }
        public override string ToString()
        {
            return $"ResourceUsageReportForJobModel({base.ToString()}; JobId: {JobId})";
        }
    }
}

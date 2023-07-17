using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

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

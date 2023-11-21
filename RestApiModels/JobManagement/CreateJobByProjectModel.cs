using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CreateJobByProjectModel")]
    public class CreateJobByProjectModel : SessionCodeModel
    {
        [DataMember(Name = "JobSpecification")]
        public JobSpecificationExt JobSpecification { get; set; }
        public override string ToString()
        {
            return $"CreateJobModel({base.ToString()}; JobSpecification: {JobSpecification})";
        }
    }
}

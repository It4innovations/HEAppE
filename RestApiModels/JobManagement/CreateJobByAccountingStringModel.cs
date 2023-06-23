using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CreateJobByAccountingStringModel")]
    public class CreateJobByAccountingStringModel : SessionCodeModel
    {
        [DataMember(Name = "JobSpecification")]
        public JobSpecificationByAccountingStringExt JobSpecification { get; set; }
        public override string ToString()
        {
            return $"CreateJobModel({base.ToString()}; JobSpecification: {JobSpecification})";
        }
    }
}

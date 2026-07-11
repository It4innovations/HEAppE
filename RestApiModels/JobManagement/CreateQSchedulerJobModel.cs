using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "CreateQSchedulerJobModel")]
public class CreateQSchedulerJobModel : SessionCodeModel
{
    [DataMember(Name = "JobSpecification")]
    public QSchedulerJobSpecificationExt JobSpecification { get; set; }
}

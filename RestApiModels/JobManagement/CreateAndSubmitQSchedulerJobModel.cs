using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "CreateAndSubmitQSchedulerJobModel")]
public class CreateAndSubmitQSchedulerJobModel : SessionCodeModel
{
    [DataMember(Name = "JobSpecification")]
    public QSchedulerJobSpecificationExt JobSpecification { get; set; }
}

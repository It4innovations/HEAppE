using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model to create job by project
/// </summary>
[DataContract(Name = "CreateJobByProjectModel")]
[Description("Model to create job by project")]
public class CreateJobByProjectModel : SessionCodeModel
{
    /// <summary>
    /// Job specification model
    /// </summary>
    [DataMember(Name = "JobSpecification")]
    [Description("Job specification model")]
    public JobSpecificationExt JobSpecification { get; set; }

    public override string ToString()
    {
        return $"CreateJobModel({base.ToString()}; JobSpecification: {JobSpecification})";
    }
}
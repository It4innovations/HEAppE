using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for retrieving list of jobs for current user
/// </summary>
[DataContract(Name = "ListJobsForCurrentUserModel")]
[Description("Model for retrieving list of jobs for current user")]
public class ListJobsForCurrentUserModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"ListJobsForCurrentUserModel({base.ToString()})";
    }
}
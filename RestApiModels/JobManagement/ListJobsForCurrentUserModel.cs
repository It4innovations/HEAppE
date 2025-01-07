using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

[DataContract(Name = "ListJobsForCurrentUserModel")]
public class ListJobsForCurrentUserModel : SessionCodeModel
{
    public override string ToString()
    {
        return $"ListJobsForCurrentUserModel({base.ToString()})";
    }
}
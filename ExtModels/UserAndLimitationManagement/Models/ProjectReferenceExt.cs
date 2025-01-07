using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "ProjectReferenceExt")]
public class ProjectReferenceExt
{
    [Required]
    [DataMember(Name = "Project")]
    public ProjectExt Project { get; set; }

    [DataMember(Name = "CoresUsed")] public AdaptorUserRoleExt Role { get; set; }

    public override string ToString()
    {
        return $"ProjectReferenceExt(project={Project}; Role={Role};)";
    }
}
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Project reference ext
/// </summary>
[DataContract(Name = "ProjectReferenceExt")]
[Description("Project reference ext")]
public class ProjectReferenceExt
{
    /// <summary>
    /// Project
    /// </summary>
    [Required]
    [DataMember(Name = "Project")]
    [Description("Project")]
    public ProjectExt Project { get; set; }

    /// <summary>
    /// Used cores
    /// </summary>
    [DataMember(Name = "CoresUsed")]
    [Description("Used cores")]
    public AdaptorUserRoleExt Role { get; set; }

    public override string ToString()
    {
        return $"ProjectReferenceExt(project={Project}; Role={Role};)";
    }
}
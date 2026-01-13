using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Assign adaptor user to project model
/// </summary>
[DataContract(Name = "AssignAdaptorUserToProjectModel")]
[Description("Assign adaptor user to project model")]
public class AssignAdaptorUserToProjectModel : SessionCodeModel
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(100)]
    [Description("Username")]
    public string Username { get; set; }
    
    /// <summary>
    /// Project Id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("ProjectId")]
    public long ProjectId { get; set; }
    
    /// <summary>
    /// Role in the project
    /// </summary>
    [DataMember(Name = "Role", IsRequired = true)]
    [Description("Role")]
    public AdaptorUserRoleType Role { get; set; }
    
    public override string ToString()
    {
        return $"AssignAdaptorUserToProjectModel: Username={Username}, ProjectId={ProjectId}, Role={Role}";
    }
}
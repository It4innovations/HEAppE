using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Adaptor user role ext
/// </summary>
[DataContract(Name = "AdaptorUserRoleExt")]
[Description("Adaptor user role ext")]
public class AdaptorUserRoleExt
{
    /// <summary>
    /// Name
    /// </summary>
    [Required]
    [DataMember(Name = "Name")]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [Required]
    [DataMember(Name = "Description")]
    [StringLength(200)]
    [Description("Description")]
    public string Description { get; set; }

    public override string ToString()
    {
        return $"AdaptorUserRoleExt(name={Name}; description={Description};)";
    }
}
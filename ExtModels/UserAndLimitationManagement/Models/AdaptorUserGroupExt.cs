using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Adaptor user group ext
/// </summary>
[DataContract(Name = "AdaptorUserGroupExt")]
[Description("Adaptor user group ext")]
public class AdaptorUserGroupExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description")]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Project
    /// </summary>
    [DataMember(Name = "Project")]
    [Description("Project")]
    public ProjectExt Project { get; set; }

    /// <summary>
    /// Array of roles
    /// </summary>
    [DataMember(Name = "Roles")]
    [Description("Array of roles")]
    public string[] Roles { get; set; }

    public override string ToString()
    {
        return $"AdaptorUserGroupExt(id={Id}; name={Name}; description={Description}; Project={Project})";
    }
}
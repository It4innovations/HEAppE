using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// System role assignment ext model
/// </summary>
[DataContract(Name = "SystemRoleAssignmentExt")]
[Description("System role assignment ext model")]
public class SystemRoleAssignmentExt
{
    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username")]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Role name
    /// </summary>
    [DataMember(Name = "Role")]
    [Description("Role name")]
    public string Role { get; set; }

    /// <summary>
    /// Assignment source (Appsettings, Dynamic, or Both)
    /// </summary>
    [DataMember(Name = "Source")]
    [Description("Assignment source (Appsettings, Dynamic, or Both)")]
    public string Source { get; set; }
}

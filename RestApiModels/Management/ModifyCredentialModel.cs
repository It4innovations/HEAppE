#nullable enable
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify credential model
/// </summary>
[DataContract(Name = "ModifyCredentialModel")]
[Description("Modify credential model")]
public class ModifyCredentialModel
{
    // --- Common Properties ---

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Session code
    /// </summary>
    [DataMember(Name = "SessionCode", IsRequired = true)]
    [Description("Session code")]
    public string SessionCode { get; set; }

    /// <summary>
    /// Adaptor user id (optional, for project managers acting on behalf of other users)
    /// </summary>
    [DataMember(Name = "AdaptorUserId", IsRequired = false)]
    [Description("Adaptor user id")]
    public long? AdaptorUserId { get; set; }

    /// <summary>
    /// Old username
    /// </summary>
    [DataMember(Name = "OldUsername", IsRequired = true)]
    [Description("Old username")]
    public string OldUsername { get; set; }

    /// <summary>
    /// New username
    /// </summary>
    [DataMember(Name = "NewUsername", IsRequired = true)]
    [Description("New username")]
    public string NewUsername { get; set; }


    // --- Password Specific Properties ---

    /// <summary>
    /// New password
    /// </summary>
    [DataMember(Name = "NewPassword", IsRequired = false)]
    [Description("New password")]
    public string? NewPassword { get; set; }
}

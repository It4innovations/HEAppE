using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Remove credential model
/// </summary>
[DataContract(Name = "RemoveCredentialModel")]
[Description("Remove credential model")]
public class RemoveCredentialModel
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
    /// Username
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [Description("Username")]
    public string Username { get; set; }
}

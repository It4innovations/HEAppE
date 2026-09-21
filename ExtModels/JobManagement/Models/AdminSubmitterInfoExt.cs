using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Submitter info for admin report
/// </summary>
[DataContract(Name = "AdminSubmitterInfoExt")]
[Description("Submitter info for admin report")]
public class AdminSubmitterInfoExt
{
    /// <summary>
    /// User Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("User Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Username
    /// </summary>
    [DataMember(Name = "Username")]
    [Description("Username")]
    public string Username { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    [DataMember(Name = "Email")]
    [Description("Email")]
    public string Email { get; set; }

    /// <summary>
    /// Identity Provider Subject ID (e.g. Keycloak / OIDC SID)
    /// </summary>
    [DataMember(Name = "IdpSid")]
    [Description("Identity Provider Subject ID (e.g. Keycloak / OIDC SID)")]
    public string IdpSid { get; set; }

    public override string ToString()
    {
        return $"AdminSubmitterInfoExt(id={Id}; username={Username}; email={Email}; idpSid={IdpSid})";
    }
}

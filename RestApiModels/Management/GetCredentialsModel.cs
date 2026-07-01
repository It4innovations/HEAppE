using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Get credentials model
/// </summary>
[DataContract(Name = "GetCredentialsModel")]
[Description("Get credentials model")]
public class GetCredentialsModel
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
    [DataMember(Name = "SessionCode", IsRequired = false)]
    [Description("Session code")]
    public string SessionCode { get; set; }

    /// <summary>
    /// Adaptor user id (optional, for project managers acting on behalf of other users)
    /// </summary>
    [DataMember(Name = "AdaptorUserId", IsRequired = false)]
    [Description("Adaptor user id")]
    public long? AdaptorUserId { get; set; }
}

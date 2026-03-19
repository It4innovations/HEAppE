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
    [DataMember(Name = "SessionCode", IsRequired = true)]
    [Description("Session code")]
    public string SessionCode { get; set; }
}

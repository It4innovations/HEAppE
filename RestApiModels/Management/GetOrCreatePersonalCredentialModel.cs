using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Model for retrieving or auto-generating personal cluster authentication credentials in 1:1 mapping projects.
/// </summary>
[DataContract(Name = "GetOrCreatePersonalCredentialModel")]
[Description("Model for retrieving or generating personal 1:1 credentials")]
public class GetOrCreatePersonalCredentialModel
{
    /// <summary>
    /// Project identifier.
    /// </summary>
    [Required]
    [DataMember(Name = "ProjectId")]
    [Description("Project ID")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Session code (optional when using Bearer authentication header).
    /// </summary>
    [DataMember(Name = "SessionCode")]
    [Description("Session code (optional when using Bearer token)")]
    public string? SessionCode { get; set; }
}

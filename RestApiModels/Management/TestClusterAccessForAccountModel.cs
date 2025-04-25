using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Test cluster access for account model
/// </summary>
[DataContract(Name = "TestClusterAccessForAccountModel")]
[Description("Test cluster access for account model")]
public class TestClusterAccessForAccountModel : SessionCodeModel
{
    /// <summary>
    /// User name
    /// </summary>
    [DataMember(Name = "Username", IsRequired = true)]
    [Description("User name")]
    public string Username { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }
}
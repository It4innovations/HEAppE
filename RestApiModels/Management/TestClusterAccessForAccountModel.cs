using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "TestClusterAccessForAccountModel")]
public class TestClusterAccessForAccountModel : SessionCodeModel
{
    [DataMember(Name = "Username", IsRequired = true)]
    public string Username { get; set; }

    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }
}
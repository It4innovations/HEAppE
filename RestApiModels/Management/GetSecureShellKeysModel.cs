using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "GetSecureShellKeysModel")]
public class GetSecureShellKeysModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }
}
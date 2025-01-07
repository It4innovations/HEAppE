using System.Collections.Generic;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateSecureShellKeyModel")]
public class CreateSecureShellKeyModel : SessionCodeModel
{
    [DataMember(Name = "Credentials", IsRequired = true)]
    public List<SshKeyUserCredentialsModel> Credentials { get; set; }

    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }
}
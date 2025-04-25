using System;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "RemoveSecureShellKeyModel")]
[Obsolete]
public class RemoveSecureShellKeyModelObsolete : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    [DataMember(Name = "PublicKey", IsRequired = true)]
    public string PublicKey { get; set; }
}
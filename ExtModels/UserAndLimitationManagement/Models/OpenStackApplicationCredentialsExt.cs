using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "OpenStackApplicationCredentialsExt")]
public class OpenStackApplicationCredentialsExt
{
    [DataMember(Name = "ApplicationCredentialsId")]
    public string ApplicationCredentialsId { get; set; }

    [DataMember(Name = "ApplicationCredentialsSecret")]
    public string ApplicationCredentialsSecret { get; set; }
}
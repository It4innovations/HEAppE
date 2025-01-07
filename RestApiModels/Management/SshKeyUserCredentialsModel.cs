using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

public class SshKeyUserCredentialsModel
{
    [DataMember(Name = "Username", IsRequired = true)]
    [StringLength(50)]
    public string Username { get; set; }

    [DataMember(Name = "Password", IsRequired = false)]
    [StringLength(50)]
    public string Password { get; set; }
}
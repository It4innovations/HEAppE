using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Authentication credentials ext
/// </summary>
[DataContract(Name = "AuthenticationCredentialsExt")]
[Description("Authentication credentials ext")]
public class AuthenticationCredentialsExt
{
    /// <summary>
    /// User name
    /// </summary>
    [DataMember(Name = "UserName")]
    [StringLength(100)]
    [Description("User name")]
    public string Username { get; set; }

    public override string ToString()
    {
        return $"""AuthenticationCredentialsExt(Username="{Username}")""";
    }
}
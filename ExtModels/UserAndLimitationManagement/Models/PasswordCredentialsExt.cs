using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Password credentials ext
/// </summary>
[DataContract(Name = "PasswordCredentialsExt")]
[Description("Password credentials ext")]
public class PasswordCredentialsExt : AuthenticationCredentialsExt
{
    /// <summary>
    /// Password
    /// </summary>
    [DataMember(Name = "Password")]
    [StringLength(50)]
    [Description("Password")]
    public string Password { get; set; }

    public override string ToString()
    {
        return $"PasswordCredentialsExt({base.ToString()}; password={Password})";
    }
}
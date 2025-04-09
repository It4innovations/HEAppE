using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model to authenticate with password
/// </summary>
[DataContract(Name = "AuthenticateUserPasswordModel")]
[Description("Model to authenticate with password")]
public class AuthenticateUserPasswordModel
{
    /// <summary>
    /// Password credentials model
    /// </summary>
    [DataMember(Name = "Credentials")]
    [Description("Password credentials model")]
    public PasswordCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserPasswordModel({base.ToString()}; Credentials: {Credentials})";
    }
}
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model to authenticate with lexis token
/// </summary>
[DataContract(Name = "AuthenticateLexisTokenModel")]
[Description("Model to authenticate with lexis token")]
public class AuthenticateLexisTokenModel
{
    /// <summary>
    /// Lexis credentials model
    /// </summary>
    [DataMember(Name = "Credentials")]
    [Description("Lexis credentials model")]
    public LexisCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserOpenIdModel({base.ToString()}; Credentials: {Credentials})";
    }
}
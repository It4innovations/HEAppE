using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model to authenticate with OpenId
/// </summary>
[DataContract(Name = "AuthenticateUserOpenIdModel")]
[Description("Model to authenticate with OpenId")]
public class AuthenticateUserOpenIdModel
{
    /// <summary>
    /// OpenId credentials model
    /// </summary>
    [DataMember(Name = "Credentials")]
    [Description("OpenId credentials model")]
    public OpenIdCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserOpenIdModel({base.ToString()}; Credentials: {Credentials})";
    }
}
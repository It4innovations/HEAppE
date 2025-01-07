using System.Runtime.Serialization;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

[DataContract(Name = "AuthenticateUserOpenIdModel")]
public class AuthenticateUserOpenIdModel
{
    [DataMember(Name = "Credentials")] public OpenIdCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserOpenIdModel({base.ToString()}; Credentials: {Credentials})";
    }
}
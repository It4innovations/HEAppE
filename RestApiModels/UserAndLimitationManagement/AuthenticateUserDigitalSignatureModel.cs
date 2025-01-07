using System.Runtime.Serialization;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

[DataContract(Name = "AuthenticateUserDigitalSignatureModel")]
public class AuthenticateUserDigitalSignatureModel
{
    [DataMember(Name = "Credentials")] public DigitalSignatureCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserDigitalSignatureModel({base.ToString()}; Credentials: {Credentials})";
    }
}
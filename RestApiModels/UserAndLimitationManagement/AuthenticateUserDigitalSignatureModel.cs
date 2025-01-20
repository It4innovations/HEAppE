using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;

namespace HEAppE.RestApiModels.UserAndLimitationManagement;

/// <summary>
/// Model to authenticate with digital signature
/// </summary>
[DataContract(Name = "AuthenticateUserDigitalSignatureModel")]
[Description("Model to authenticate with digital signature")]
public class AuthenticateUserDigitalSignatureModel
{
    /// <summary>
    /// Digital signature credentials model
    /// </summary>
    [DataMember(Name = "Credentials")]
    [Description("Digital signature credentials model")]
    public DigitalSignatureCredentialsExt Credentials { get; set; }

    public override string ToString()
    {
        return $"AuthenticateUserDigitalSignatureModel({base.ToString()}; Credentials: {Credentials})";
    }
}
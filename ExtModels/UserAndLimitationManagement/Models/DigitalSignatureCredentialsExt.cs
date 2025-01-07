using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

[DataContract(Name = "DigitalSignatureCredentialsExt")]
public class DigitalSignatureCredentialsExt : AuthenticationCredentialsExt
{
    [DataMember(Name = "Noise")] public string Noise { get; set; }

    [DataMember(Name = "DigitalSignature")]
    public sbyte[] DigitalSignature { get; set; }

    public override string ToString()
    {
        return $"DigitalSignatureCredentialsExt({base.ToString()}; noise={Noise}; digitalSignature={DigitalSignature})";
    }
}
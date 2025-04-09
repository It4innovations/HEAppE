using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models;

/// <summary>
/// Digital signature credentials ext
/// </summary>
[DataContract(Name = "DigitalSignatureCredentialsExt")]
[Description("Digital signature credentials ext")]
public class DigitalSignatureCredentialsExt : AuthenticationCredentialsExt
{
    /// <summary>
    /// Noise
    /// </summary>
    [DataMember(Name = "Noise")]
    [Description("Noise")]
    public string Noise { get; set; }
    
    /// <summary>
    /// Digital signature
    /// </summary>
    [DataMember(Name = "DigitalSignature")]
    [Description("Digital signature")]
    public sbyte[] DigitalSignature { get; set; }

    public override string ToString()
    {
        return $"DigitalSignatureCredentialsExt({base.ToString()}; noise={Noise}; digitalSignature={DigitalSignature})";
    }
}
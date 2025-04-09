using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

/// <summary>
/// Model to end file transfer
/// </summary>
[DataContract(Name = "EndFileTransferModel")]
[Description("Model to end file transfer")]
public class EndFileTransferModel : SubmittedJobInfoModel
{
    /// <summary>
    /// Public key
    /// </summary>
    [DataMember(Name = "PublicKey")]
    [Description("Public key")]
    public string PublicKey { get; set; }

    public override string ToString()
    {
        return $"EndFileTransferModel({base.ToString()}; PublicKey: {PublicKey})";
    }
}
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer;

[DataContract(Name = "EndFileTransferModel")]
public class EndFileTransferModel : SubmittedJobInfoModel
{
    [DataMember(Name = "PublicKey")] public string PublicKey { get; set; }

    public override string ToString()
    {
        return $"EndFileTransferModel({base.ToString()}; PublicKey: {PublicKey})";
    }
}
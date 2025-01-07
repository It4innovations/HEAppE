using System.Runtime.Serialization;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer;

[DataContract(Name = "EndDataTransferModel")]
public class EndDataTransferModel : SessionCodeModel
{
    [DataMember(Name = "UsedTransferMethod")]
    public DataTransferMethodExt UsedTransferMethod { get; set; }

    public override string ToString()
    {
        return $"EndDataTransferModel({base.ToString()}; UsedTransferMethod: {UsedTransferMethod})";
    }
}
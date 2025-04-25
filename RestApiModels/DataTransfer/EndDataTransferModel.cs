using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer;

/// <summary>
/// End data transfer model
/// </summary>
[DataContract(Name = "EndDataTransferModel")]
[Description("End data transfer model")]
public class EndDataTransferModel : SessionCodeModel
{
    /// <summary>
    /// Used data transfer method model
    /// </summary>
    [DataMember(Name = "UsedTransferMethod")]
    [Description("Used data transfer method model")]
    public DataTransferMethodExt UsedTransferMethod { get; set; }

    public override string ToString()
    {
        return $"EndDataTransferModel({base.ToString()}; UsedTransferMethod: {UsedTransferMethod})";
    }
}
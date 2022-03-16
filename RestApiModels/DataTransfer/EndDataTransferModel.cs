using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "EndDataTransferModel")]
    public class EndDataTransferModel : SessionCodeModel
    {
        [DataMember(Name = "UsedTransferMethod")]
        public DataTransferMethodExt UsedTransferMethod { get; set; }
    }
}

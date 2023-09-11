using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;
using System;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "EndFileTransferModelOveral")]
    [Obsolete]
    public class EndFileTransferModelOveral : SubmittedJobInfoModel
    {
        [DataMember(Name = "UsedTransferMethod")]
        public FileTransferMethodExt UsedTransferMethod { get; set; }
        public override string ToString()
        {
            return $"EndFileTransferModel({base.ToString()}; UsedTransferMethod: {UsedTransferMethod})";
        }
    }
}

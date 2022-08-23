using HEAppE.ExtModels.FileTransfer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "EndFileTransferModel")]
    public class EndFileTransferModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "UsedTransferMethod")]
        public FileTransferMethodExt UsedTransferMethod { get; set; }
        public override string ToString()
        {
            return $"EndFileTransferModel({base.ToString()}; UsedTransferMethod: {UsedTransferMethod})";
        }
    }
}

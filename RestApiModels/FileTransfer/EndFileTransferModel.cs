using HEAppE.ExtModels.FileTransfer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "EndFileTransferModel")]
    public class EndFileTransferModel
    {
        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "UsedTransferMethod")]
        public FileTransferMethodExt UsedTransferMethod { get; set; }

        [DataMember(Name = "SessionCode"), StringLength(50)]
        public string SessionCode { get; set; }
    }
}

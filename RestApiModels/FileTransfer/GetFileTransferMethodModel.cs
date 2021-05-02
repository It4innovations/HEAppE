using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "GetFileTransferMethodModel")]
    public class GetFileTransferMethodModel
    {
        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "SessionCode"), StringLength(50)]
        public string SessionCode { get; set; }
    }
}

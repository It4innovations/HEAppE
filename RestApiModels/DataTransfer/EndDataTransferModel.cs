using HEAppE.ExtModels.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "EndDataTransferModel")]
    public class EndDataTransferModel
    {
        [DataMember(Name = "UsedTransferMethod")]
        public DataTransferMethodExt UsedTransferMethod { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

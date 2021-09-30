using HEAppE.ExtModels.DataTransfer.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.DataTransfer
{
    [DataContract(Name = "EndDataTransferModel")]
    public class EndDataTransferModel : SessionCodeModel
    {
        [DataMember(Name = "UsedTransferMethod")]
        public DataTransferMethodExt UsedTransferMethod { get; set; }

    }
}

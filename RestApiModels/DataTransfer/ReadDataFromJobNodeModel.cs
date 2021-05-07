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
    [DataContract(Name = "ReadDataFromJobNodeModel")]
    public class ReadDataFromJobNodeModel : SubmittedJobInfoModel
    {

        [DataMember(Name = "IpAddress"), StringLength(45)]
        public string IpAddress { get; set; }

    }
}

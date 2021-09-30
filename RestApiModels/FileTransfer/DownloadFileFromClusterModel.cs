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
    [DataContract(Name = "DownloadFileFromClusterModel")]
    public class DownloadFileFromClusterModel : SubmittedJobInfoModel
    {

        [DataMember(Name = "RelativeFilePath"), StringLength(50)]
        public string RelativeFilePath { get; set; }

    }
}

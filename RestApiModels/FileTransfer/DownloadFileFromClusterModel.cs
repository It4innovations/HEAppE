using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "DownloadFileFromClusterModel")]
    public class DownloadFileFromClusterModel
    {
        [DataMember(Name = "SubmittedJobInfoId")]
        public long SubmittedJobInfoId { get; set; }

        [DataMember(Name = "RelativeFilePath")]
        public string RelativeFilePath { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApiModels.AbstractModels;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.FileTransfer
{
    [DataContract(Name = "DownloadPartsOfJobFilesFromClusterModel")]
    public class DownloadPartsOfJobFilesFromClusterModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "TaskFileOffsets")]
        public TaskFileOffsetExt[] TaskFileOffsets { get; set; }
        public override string ToString()
        {
            return $"DownloadPartsOfJobFilesFromClusterModel({base.ToString()}; TaskFileOffsets: {string.Join("; ", TaskFileOffsets.ToList())})";
        }
    }
}

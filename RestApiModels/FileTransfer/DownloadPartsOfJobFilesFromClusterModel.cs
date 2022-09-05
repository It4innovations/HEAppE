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

using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CopyJobDataToTempModel")]
    public class CopyJobDataToTempModel : SubmittedJobInfoModel
    {
        [DataMember(Name = "Path"), StringLength(50)]
        public string Path { get; set; }
        public override string ToString()
        {
            return $"CopyJobDataToTempModel({base.ToString()}; Path: {Path})";
        }
    }
}

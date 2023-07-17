using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CopyJobDataFromTempModel")]
    public class CopyJobDataFromTempModel : CreatedJobInfoModel
    {
        [DataMember(Name = "TempSessionCode"), StringLength(50)]
        public string TempSessionCode { get; set; }
        public override string ToString()
        {
            return $"CopyJobDataFromTempModel({base.ToString()}; TempSessionCode: {TempSessionCode})";
        }
    }
}

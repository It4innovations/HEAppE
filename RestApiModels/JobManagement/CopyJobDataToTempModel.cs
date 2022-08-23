using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using HEAppE.RestApiModels.AbstractModels;

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

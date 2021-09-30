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
    [DataContract(Name = "CopyJobDataFromTempModel")]
    public class CopyJobDataFromTempModel : CreatedJobInfoModel
    {
        [DataMember(Name = "TempSessionCode"), StringLength(50)]
        public string TempSessionCode { get; set; }
    }
}

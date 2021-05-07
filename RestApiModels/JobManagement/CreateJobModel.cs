using HEAppE.ExtModels.JobManagement.Models;
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
    [DataContract(Name = "CreateJobModel")]
    public class CreateJobModel : SessionCodeModel
    {
        [DataMember(Name = "JobSpecification")]
        public JobSpecificationExt JobSpecification { get; set; }
    }
}

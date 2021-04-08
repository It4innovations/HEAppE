using HEAppE.ExtModels.JobManagement.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.JobManagement
{
    [DataContract(Name = "CreateJobModel")]
    public class CreateJobModel
    {
        [DataMember(Name = "JobSpecification")]
        public JobSpecificationExt JobSpecification { get; set; }

        [DataMember(Name = "SessionCode")]
        public string SessionCode { get; set; }
    }
}

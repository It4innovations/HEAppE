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
    [DataContract(Name = "CreateJobByProjectModel")]
    public class CreateJobByProjectModel : SessionCodeModel
    {
        [DataMember(Name = "JobSpecification")]
        public JobSpecificationByProjectExt JobSpecification { get; set; }
        public override string ToString()
        {
            return $"CreateJobModel({base.ToString()}; JobSpecification: {JobSpecification})";
        }
    }
}

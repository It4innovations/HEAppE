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
    [DataContract(Name = "CreateJobByAccountingStringModel")]
    public class CreateJobByAccountingStringModel : SessionCodeModel
    {
        [DataMember(Name = "JobSpecification")]
        public JobSpecificationByAccountingStringExt JobSpecification { get; set; }
        public override string ToString()
        {
            return $"CreateJobModel({base.ToString()}; JobSpecification: {JobSpecification})";
        }
    }
}

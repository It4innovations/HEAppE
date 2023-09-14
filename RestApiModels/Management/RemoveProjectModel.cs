using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.AbstractModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RemoveProjectModel")]
    public class RemoveProjectModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }
    }
}

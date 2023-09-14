using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.AbstractModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateProjectAssignmentToClusterModel")]
    public class CreateProjectAssignmentToClusterModel : SessionCodeModel
    {
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }

        [DataMember(Name = "ClusterId", IsRequired = true)]
        public long ClusterId { get; set; }

        [DataMember(Name = "LocalBasepath", IsRequired = true), StringLength(100)]
        public string LocalBasepath { get; set; }

    }
}

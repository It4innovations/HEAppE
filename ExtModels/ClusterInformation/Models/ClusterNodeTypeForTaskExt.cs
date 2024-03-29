﻿using HEAppE.ExtModels.JobManagement.Models;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ClusterNodeTypeForTaskExt")]
    public class ClusterNodeTypeForTaskExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "Projects")]
        public ProjectForTaskExt Project { get; set; }

        public override string ToString()
        {
            return $"ClusterNodeTypeExt(id={Id}; name={Name}; description={Description}; project={Project})";
        }
    }
}

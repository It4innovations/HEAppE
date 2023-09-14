using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ClusterProjectExt")]
    public class ClusterProjectExt
    {
        [DataMember(Name = "Id")]
        public long ClusterId { get; set; }

        [DataMember(Name = "ProjectId")]
        public long ProjectId { get; set; }

        [DataMember(Name = "LocalBasepath"), StringLength(100)]
        public string LocalBasepath { get; set; }

        [DataMember(Name = "CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [DataMember(Name = "ModifiedAt")]
        public DateTime? ModifiedAt { get; set; }

        [DataMember(Name = "IsDeleted")]
        public bool IsDeleted { get; set; } = false;

        public override string ToString()
        {
            return $"""ClusterProjectExt: ClusterId={ClusterId}, ProjectId={ProjectId}, LocalBasepath={LocalBasepath}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}" """;
        }
    }
}

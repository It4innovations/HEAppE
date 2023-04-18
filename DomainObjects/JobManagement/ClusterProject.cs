using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("ClusterProject")]
    public class ClusterProject : IdentifiableDbEntity
    {
        public long ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }

        public long ProjectId { get; set; }
        public virtual Project Project { get; set; }

        [Required]
        [StringLength(100)]
        public string LocalBasepath { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;
        public virtual List<ClusterProjectCredentials> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredentials>();

        //TODO: override tostring
    }
}

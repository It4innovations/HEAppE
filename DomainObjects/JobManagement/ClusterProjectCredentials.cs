using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("ClusterProjectCredentials")]
    public class ClusterProjectCredentials
    {
        public long ClusterProjectId { get; set; }
        public virtual ClusterProject ClusterProject { get; set; }

        public long ClusterAuthenticationCredentialsId { get; set; }
        public virtual ClusterAuthenticationCredentials ClusterAuthenticationCredentials { get; set; }

        public bool IsServiceAccount { get; set; } = false;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedAt { get; set; }

        [Required]
        public bool IsDeleted { get; set; } = false;

        //TODO: override tostring
    }
}

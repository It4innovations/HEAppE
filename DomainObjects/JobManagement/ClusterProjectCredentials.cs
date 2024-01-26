using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("ClusterProjectCredentials")]
    public class ClusterProjectCredential
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


        public override string ToString()
        {
            return $"""ClusterProjectCredentials: ClusterProject={ClusterProject}, ClusterAuthenticationCredentials={ClusterAuthenticationCredentials}, IsServiceAccount={IsServiceAccount}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}" """;
        }
    }
}
using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Table("ClusterAuthenticationCredentials")]
    public class ClusterAuthenticationCredentials : IdentifiableDbEntity
    {
        #region Properties
        [Required]
        [StringLength(50)]
        public string Username { get; set; }

        [StringLength(50)]
        public string Password { get; set; }

        [StringLength(200)]
        public string PrivateKeyFile { get; set; }

        [StringLength(50)]
        public string PrivateKeyPassword { get; set; }

        [Required]
        public ClusterAuthenticationCredentialsAuthType AuthenticationType { get; set; }

        public virtual List<ClusterProjectCredentials> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredentials>();
        #endregion

        #region Public Methods
        public Cluster GetClusterForProject(long clusterId, long projectId)
        {
            var cluster = ClusterProjectCredentials.Find(x => x.ClusterId == clusterId
                                                                && x.ProjectId == projectId)?.ClusterProject.Cluster;
            return cluster;
        }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterAuthenticationCredentials: Username={Username}, AuthenticationType={nameof(AuthenticationType)}";
        }
        #endregion
    }
}
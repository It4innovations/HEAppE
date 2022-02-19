using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [ForeignKey("Cluster")]
        public long? ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterAuthenticationCredentials: Username={Username}, AuthenticationType={nameof(AuthenticationType)}";
        }
        #endregion
    }
}
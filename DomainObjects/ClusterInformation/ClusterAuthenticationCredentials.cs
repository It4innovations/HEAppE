using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Table("ClusterAuthenticationCredentials")]
    public class ClusterAuthenticationCredentials : IdentifiableDbEntity, ISoftDeletableEntity
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

        [Required]
        public FileTransferCipherType CipherType { get; set; } = FileTransferCipherType.Unknown;

        [StringLength(200)]
        public string PublicKeyFingerprint { get; set; }

        [Required]
        public bool IsGenerated { get; set; } = false;

        [Required]
        public bool IsDeleted { get; set; } = false;

        public virtual List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new List<ClusterProjectCredential>();
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"ClusterAuthenticationCredentials: Username={Username}, AuthenticationType={nameof(AuthenticationType)}, CipherType={CipherType}";
        }
        #endregion
    }
}
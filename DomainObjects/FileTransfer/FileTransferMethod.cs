using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.FileTransfer
{
    [Table("FileTransferMethod")]
    public class FileTransferMethod : IdentifiableDbEntity
    {
        [Required]
        [StringLength(50)]
        public string ServerHostname { get; set; }

        public FileTransferProtocol Protocol { get; set; }

        [ForeignKey("Cluster")]
        public long ClusterId { get; set; }

        [Required]
        public virtual Cluster Cluster { get; set; }

        [NotMapped]
        public string SharedBasePath { get; set; }

        [NotMapped]
        public FileTransferCipherType FileTransferCipherType { get; set; }

        [NotMapped]
        public AuthenticationCredentials Credentials { get; set; }

        public override string ToString()
        {
            return $"Cluster: Id={Id}, ServerHostname={ServerHostname}, Protocol={Protocol}, Cluster={Cluster}, Credentials={Credentials}";
        }
    }
}
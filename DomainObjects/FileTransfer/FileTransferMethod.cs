using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;

namespace HEAppE.DomainObjects.FileTransfer;

[Table("FileTransferMethod")]
public class FileTransferMethod : IdentifiableDbEntity, ISoftDeletableEntity
{
    [Required] [StringLength(50)] public string ServerHostname { get; set; }

    public FileTransferProtocol Protocol { get; set; }

    public int? Port { get; set; }

    [ForeignKey("Cluster")] public long ClusterId { get; set; }

    [Required] public virtual Cluster Cluster { get; set; }

    [NotMapped] public string SharedBasePath { get; set; }

    [NotMapped] public AuthenticationCredentials Credentials { get; set; }

    [Required] public bool IsDeleted { get; set; } = false;

    public override string ToString()
    {
        return
            $"Cluster: Id={Id}, ServerHostname={ServerHostname}, Protocol={Protocol}, Port={Port}, Cluster={Cluster}, Credentials={Credentials}";
    }
}
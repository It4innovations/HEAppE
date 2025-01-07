using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement;

[Table("OpenStackSession")]
public class OpenStackSession : IdentifiableDbEntity
{
    [Required] public string ApplicationCredentialsId { get; set; }

    [Required] public string ApplicationCredentialsSecret { get; set; }

    [Required] public DateTime AuthenticationTime { get; set; }

    [Required] public DateTime ExpirationTime { get; set; }

    [Required] [ForeignKey("AdaptorUser")] public long UserId { get; set; }

    public virtual AdaptorUser User { get; set; }
}
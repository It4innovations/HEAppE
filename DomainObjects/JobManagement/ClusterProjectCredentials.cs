using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ClusterProjectCredentials")]
public class ClusterProjectCredential : ISoftDeletableEntity
{
    public long ClusterProjectId { get; set; }
    public virtual ClusterProject ClusterProject { get; set; }

    public long ClusterAuthenticationCredentialsId { get; set; }
    public virtual ClusterAuthenticationCredentials ClusterAuthenticationCredentials { get; set; }

    public bool IsServiceAccount { get; set; } = false;

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    [Required] public bool IsDeleted { get; set; } = false;

    public long? AdaptorUserId { get; set; }

    public virtual AdaptorUser AdaptorUser { get; set; }

    public bool IsInitialized { get; set; }

    public override string ToString()
    {
        return
            $"""ClusterProjectCredentials: ClusterProject={ClusterProject}, ClusterAuthenticationCredentials={ClusterAuthenticationCredentials}, IsServiceAccount={IsServiceAccount}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}, AdaptorUserId={AdaptorUserId}, IsInitialized={IsInitialized}""";
    }
}
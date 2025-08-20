using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ClusterProjectCredentialsCheckLog")]
public class ClusterProjectCredentialCheckLog : IdentifiableDbEntity
{
    [Required] public long ClusterProjectId { get; set; }
    public virtual ClusterProject ClusterProject { get; set; }


    [Required] public long ClusterAuthenticationCredentialsId { get; set; }
    public virtual ClusterAuthenticationCredentials ClusterAuthenticationCredentials { get; set; }


    [Required] public DateTime CheckTimestamp { get; set; } = DateTime.UtcNow;

    public bool? VaultCredentialOk { get; set; }

    public bool? ClusterConnectionOk { get; set; }

    public bool? DryRunJobOk { get; set; }

    [StringLength(500)] public string ErrorMessage { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public override string ToString()
    {
        return
            $"""ClusterProjectCredentials: Id={Id}, ClusterProject={ClusterProject}, ClusterAuthenticationCredentials={ClusterAuthenticationCredentials}, CheckTimestamp={CheckTimestamp}, VaultCredentialOk={VaultCredentialOk}, ClusterConnectionOk={ClusterConnectionOk}, DryRunJobOk={DryRunJobOk}, ErrorMessage={ErrorMessage}, CreatedAt={CreatedAt}""";
    }
}
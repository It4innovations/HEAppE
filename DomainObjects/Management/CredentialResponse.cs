using System.Linq;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.Management;

public class CredentialResponse
{
    public long Id { get; set; }
    public string Username { get; set; }
    public ClusterAuthenticationCredentialsAuthType AuthType { get; set; }
    public bool IsGenerated { get; set; }
    public string? PublicKeyFingerprint { get; set; }
    public string? PublicKeyExt { get; set; }

    public long? AdaptorUserId { get; set; }

    public static CredentialResponse GetCredential(ClusterAuthenticationCredentials clusterCredentials)
    {
        return GetCredential(clusterCredentials, null);
    }

    public static CredentialResponse GetCredential(ClusterAuthenticationCredentials clusterCredentials, long projectId)
    {
        var adaptorUserId = clusterCredentials.ClusterProjectCredentials
            .FirstOrDefault(cpc => cpc.ClusterProject.ProjectId == projectId && !cpc.IsDeleted)?.AdaptorUserId;
        return GetCredential(clusterCredentials, adaptorUserId);
    }

    public static CredentialResponse GetCredential(ClusterAuthenticationCredentials clusterCredentials, long? adaptorUserId)
    {
        return new CredentialResponse
        {
            Id = clusterCredentials.Id,
            Username = clusterCredentials.Username,
            AuthType = clusterCredentials.AuthenticationType,
            IsGenerated = clusterCredentials.IsGenerated,
            PublicKeyFingerprint = clusterCredentials.PublicKeyFingerprint,
            PublicKeyExt = clusterCredentials.PublicKey,
            AdaptorUserId = adaptorUserId
        };
    }
}
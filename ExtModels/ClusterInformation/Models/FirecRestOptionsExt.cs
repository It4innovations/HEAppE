using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// FirecRest options shared among multiple clusters that connect to the same FirecREST instance.
/// It is possible to view FirecREST as a kind of proxy that allows connection to SLURM or PBS instances
/// behind it.
/// </summary>
[DataContract(Name = "FirecRestOptionsExt")]
public class FirecRestOptionsExt
{
    /// <summary>
    /// FirecREST endpoint URL
    /// </summary>
    [DataMember(Name = "Url")]
    public string Url { get; set; }

    /// <summary>
    /// FirecREST keycloak URL
    /// </summary>
    [DataMember(Name = "IdpUrl")]
    public string IdpUrl { get; set; }

    [DataMember(Name = "ExpirioMetadata")]
    public ExpirioMetadataExt ExpirioMetadata { get; set; }

    /// <summary>
    /// Expirio metadata desribing object in Expirio with user specific settings.
    /// </summary>
    [DataContract(Name = "ExpirioMetadataExt")]
    public class ExpirioMetadataExt
    {
        /// <summary>
        /// SecretName in Expirio for given FirecREST instance
        /// </summary>
        [DataMember(Name = "SecretName")]
        public string SecretName { get; set; } = string.Empty;

        /// <summary>
        /// Metadata of secret content in Expirio, allows to set names of keys in Expirio managed object.
        /// </summary>
        [DataMember(Name = "SecretContent")]
        public SecretContentExt SecretContent { get; set; } = new();

        [DataContract(Name = "ExpirioMetadataExt")]
        public class SecretContentExt
        {
            /// <summary>
            /// Key for ClientId that is being sent to identity service at IdpUrl to obtain FirecREST's JWT
            /// </summary>
            [DataMember(Name = "ClientId")]
            public string ClientId { get; set; } = "clientId";

            /// <summary>
            /// Key for ClientSecret (password) that is being sent to identity service at IdpUrl to obtain FirecREST's JWT
            /// </summary>
            [DataMember(Name = "ClientSecret")]
            public string ClientSecret { get; set; } = "clientSecret";

            /// <summary>
            /// If set to non-empty value, allows to obtain Url from Expirio (if needed)
            /// </summary>
            [DataMember(Name = "Url")]
            public string Url { get; set; } = string.Empty;

            /// <summary>
            /// If set to non-empty value, allows to obtain IdpUrl from Expirio (if needed)
            /// </summary>
            [DataMember(Name = "IdpUrl")]
            public string IdpUrl { get; set; } = string.Empty;

            public override string ToString()
            {
                return $"ClientId={ClientId}, ClientSecret={ClientSecret}, ...";
            }
        }

        public override string ToString()
        {
            return $"SecretName={SecretName}, SecretContent={SecretContent}";
        }
    }

    public override string ToString()
    {
        return $"Url={Url}, IdpUrl={IdpUrl}, ExpirioMetadata={ExpirioMetadata}";
    }
}

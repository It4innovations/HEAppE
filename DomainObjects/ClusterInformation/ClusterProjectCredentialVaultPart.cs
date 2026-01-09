using System.Text.Json;
using System.Text.Json.Serialization;

namespace HEAppE.DomainObjects.ClusterInformation;

public record ClusterProjectCredentialVaultPart(
    [property: JsonPropertyName("Id")] long Id,
    [property: JsonPropertyName("Password")] string Password,
    [property: JsonPropertyName("PrivateKey")] string PrivateKey,
    [property: JsonPropertyName("PrivateKeyPassword")] string PrivateKeyPassword,
    [property: JsonPropertyName("PrivateKeyCertificate")] string PrivateKeyCertificate
)
{
    // Default constructor prevents nulls in properties
    public ClusterProjectCredentialVaultPart() : this(-1, "", "", "", "") { }

    public static ClusterProjectCredentialVaultPart Empty => new();

    public string AsVaultDataJsonObject()
    {
        // Wraps the object into {"data": {...}} envelope
        return JsonSerializer.Serialize(new { data = this });
    }

    public static ClusterProjectCredentialVaultPart FromVaultJsonData(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Empty;

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var envelope = JsonSerializer.Deserialize<VaultEnvelope>(json, options);
            
            // Returns the inner data or Empty if null
            return envelope?.Data ?? Empty;
        }
        catch
        {
            return Empty;
        }
    }

    private record VaultEnvelope([property: JsonPropertyName("data")] ClusterProjectCredentialVaultPart Data);
}
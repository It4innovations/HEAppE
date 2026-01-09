using System.Text.Json;
using System.Text.Json.Serialization;

namespace HEAppE.DomainObjects.ClusterInformation;

public record ClusterProjectCredentialVaultPart(
    long Id,
    string Password,
    string PrivateKey,
    string PrivateKeyPassword,
    string PrivateKeyCertificate
)
{
    // Reuse options to avoid re-allocating them on every call
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ClusterProjectCredentialVaultPart Empty { get; } = new(-1, "", "", "", "");

    /// <summary>
    /// Serializes the object into a { "data": { ... } } wrapper.
    /// </summary>
    public string AsVaultDataJsonObject()
    {
        // Avoid manual string interpolation. Serialize the wrapper object directly.
        return JsonSerializer.Serialize(new { data = this }, _options);
    }

    /// <summary>
    /// Deserializes from a nested { "data": { "data": { ... } } } structure.
    /// </summary>
    public static ClusterProjectCredentialVaultPart FromVaultJsonData(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            
            // Navigate the JSON tree: data -> data
            if (doc.RootElement.TryGetProperty("data", out var level1) &&
                level1.TryGetProperty("data", out var level2))
            {
                return level2.Deserialize<ClusterProjectCredentialVaultPart>(_options) ?? Empty;
            }
        }
        catch (JsonException)
        {
            // Log or handle parsing error
        }

        return Empty;
    }
}
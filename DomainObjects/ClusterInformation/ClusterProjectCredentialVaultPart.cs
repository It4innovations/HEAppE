using System.Text.Json;
using System.Text.Json.Serialization;

namespace HEAppE.DomainObjects.ClusterInformation;

public record ClusterProjectCredentialVaultPart
{
    public long Id { get; init; }
    public string Password { get; init; }
    public string PrivateKey { get; init; }
    public string PrivateKeyPassword { get; init; }
    public string PrivateKeyCertificate { get; init; }

    // Custom constructor to handle nulls from JSON and prevent Base64 errors
    [JsonConstructor]
    public ClusterProjectCredentialVaultPart(
        long id, 
        string? password, 
        string? privateKey, 
        string? privateKeyPassword, 
        string? privateKeyCertificate)
    {
        Id = id;
        Password = password ?? string.Empty;
        PrivateKey = privateKey ?? string.Empty;
        PrivateKeyPassword = privateKeyPassword ?? string.Empty;
        PrivateKeyCertificate = privateKeyCertificate ?? string.Empty;
    }

    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static ClusterProjectCredentialVaultPart Empty => new(-1, "", "", "", "");

    public string AsVaultDataJsonObject()
    {
        // Wrap in an anonymous object to produce {"data": {...}}
        return JsonSerializer.Serialize(new { data = this }, _options);
    }

    public static ClusterProjectCredentialVaultPart FromVaultJsonData(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            
            // Navigate the "data" -> "data" hierarchy safely
            if (doc.RootElement.TryGetProperty("data", out var level1) &&
                level1.TryGetProperty("data", out var level2))
            {
                return level2.Deserialize<ClusterProjectCredentialVaultPart>(_options) ?? Empty;
            }
        }
        catch (JsonException)
        {
            // Silently return Empty if JSON is invalid to prevent background service crash
        }

        return Empty;
    }
}
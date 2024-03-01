using System.Text.Json;

namespace HEAppE.DomainObjects.ClusterInformation;


/// <summary>
/// Represents a cluster project credential vault part.
/// </summary>
public record ClusterProjectCredentialVaultPart(long Id,
        string Password,
        string PrivateKey,
        string PrivateKeyPassword
        )
{
    private ClusterProjectCredentialVaultPart() : this(-1, "", "", "") { }

    /// <summary>
    /// Gets the default empty cluster project credential vault part.
    /// </summary>
    public static ClusterProjectCredentialVaultPart Empty => new ClusterProjectCredentialVaultPart();

    public string AsVaultDataJsonObject() => $"{{ \"data\":{JsonSerializer.Serialize(this)}}}";

    public static ClusterProjectCredentialVaultPart FromVaultJsonData(string json)
    {
        var response = JsonSerializer.Deserialize<VaultResponse>(json);

        return response?.data.data ?? Empty;
    }

    private record DataPart(ClusterProjectCredentialVaultPart data);
    private record VaultResponse(DataPart data);
}


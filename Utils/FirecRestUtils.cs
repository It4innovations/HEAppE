using System.Text.Json;
using System.Text.Json.Serialization;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.Utils;

/// <summary>
/// Utility methods for FirecREST URL and endpoint construction.
/// </summary>
public static class FirecRestUtils
{
    /// <summary>
    /// Gets the base URL for the FirecREST service from the given Cluster configuration.
    /// </summary>
    public static string GetFirecRestUrl(Cluster cluster)
    {
        if (cluster == null) return string.Empty;
        string protocol = cluster.ConnectionProtocol == ClusterConnectionProtocol.Http ? "http" : "https";
        return $"{protocol}://{cluster.MasterNodeName}".TrimEnd('/');
    }

    /// <summary>
    /// Gets the userinfo URL for the FirecREST service from the given Cluster configuration.
    /// </summary>
    public static string GetUserinfoUrl(Cluster cluster)
    {
        if (cluster == null) return string.Empty;
        return $"{GetFirecRestUrl(cluster)}/status/{cluster.Name}/userinfo";
    }

    /// <summary>
    /// Parses the remote username from the userinfo JSON response payload.
    /// </summary>
    public static string? ParseUsernameFromUserinfo(string jsonContent)
    {
        if (string.IsNullOrEmpty(jsonContent)) return null;
        try
        {
            var data = JsonSerializer.Deserialize<FirecRestUserinfoResponse>(jsonContent);
            return data?.User?.Name;
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Represents the userinfo response structure from the FirecREST v2 API.
/// </summary>
public class FirecRestUserinfoResponse
{
    [JsonPropertyName("user")]
    public FirecRestUserInfo? User { get; set; }
}

/// <summary>
/// Represents the user info details from the FirecREST v2 API.
/// </summary>
public class FirecRestUserInfo
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

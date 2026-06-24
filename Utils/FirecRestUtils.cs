using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.Utils;

/// <summary>
/// Utility methods for FirecREST URL, endpoint construction, and response parsing.
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

    /// <summary>
    /// Parses the files list from the filesystem ls JSON response payload.
    /// Supports both direct array responses and wrapped output objects.
    /// </summary>
    public static List<FirecRestFileItem>? ParseLsResponse(string jsonContent)
    {
        if (string.IsNullOrEmpty(jsonContent)) return null;
        try
        {
            var trimmed = jsonContent.TrimStart();
            if (trimmed.StartsWith("["))
            {
                return JsonSerializer.Deserialize<List<FirecRestFileItem>>(jsonContent);
            }

            var response = JsonSerializer.Deserialize<FirecRestLsResponse>(jsonContent);
            return response?.Output;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parses the file content string from the filesystem view JSON response payload.
    /// Supports direct content objects and wrapped output objects.
    /// </summary>
    public static string? ParseFileContent(string jsonContent)
    {
        if (string.IsNullOrEmpty(jsonContent)) return null;
        try
        {
            var response = JsonSerializer.Deserialize<FirecRestFileContentResponse>(jsonContent);
            if (response != null)
            {
                if (!string.IsNullOrEmpty(response.Content))
                {
                    return response.Content;
                }
                if (response.Output.HasValue)
                 {
                    var output = response.Output.Value;
                    if (output.ValueKind == JsonValueKind.String)
                    {
                        return output.GetString();
                    }
                    if (output.ValueKind == JsonValueKind.Object)
                    {
                        var inner = JsonSerializer.Deserialize<FirecRestFileContent>(output.GetRawText());
                        return inner?.Content;
                    }
                }
            }
        }
        catch
        {
            // ignore and fallback
        }
        return null;
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

/// <summary>
/// Represents the file system item metadata returned by the FirecREST ls command.
/// </summary>
public class FirecRestFileItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("permissions")]
    public string Permissions { get; set; } = string.Empty;

    [JsonPropertyName("lastModified")]
    public string LastModified { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public string Size { get; set; } = string.Empty;

    [JsonPropertyName("linkTarget")]
    public string? LinkTarget { get; set; }
}

/// <summary>
/// Represents the ls response envelope from the FirecREST API.
/// </summary>
public class FirecRestLsResponse
{
    [JsonPropertyName("output")]
    public List<FirecRestFileItem>? Output { get; set; }
}

/// <summary>
/// Represents the file content details returned by the FirecREST view command.
/// </summary>
public class FirecRestFileContent
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}

/// <summary>
/// Represents the view response envelope from the FirecREST API.
/// </summary>
public class FirecRestFileContentResponse
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("output")]
    public JsonElement? Output { get; set; }
}

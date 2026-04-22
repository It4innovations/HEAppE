using System.Text.Json.Serialization;

namespace Services.Expirio.Models;

public class KerberosCredentialResponse
{
    public string FileName { get; set; }
    public string Content { get; set; }
    [JsonPropertyName("preferredUsername")]
    public string? PreferredUsername { get; set; }
    public int Size { get; set; }
    public DateTime Timestamp { get; set; }
}
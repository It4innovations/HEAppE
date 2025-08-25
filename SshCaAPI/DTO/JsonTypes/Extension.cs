using System.Text.Json.Serialization;

namespace SshCaAPI.DTO.JsonTypes
{
    public sealed class Extension
    {
        [JsonPropertyName("permit-agent-forwarding")]
        public string? PermitAgentForwarding { get; set; }

        [JsonPropertyName("permit-pty")]
        public string? PerimtPty { get; set; }
    }
}

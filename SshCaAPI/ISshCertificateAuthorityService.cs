using Microsoft.Extensions.Logging;
using SshCaAPI.DTO.JsonTypes;

namespace SshCaAPI
{
    public interface ISshCertificateAuthorityService
    {
        public Task<ConfigResponse> GetConfigAsync();
        public Task<SignResponse?> SignAsync(string publicKey, string ott, string resource, ILogger? logger);
        public Task<string?> GetPosixUsernameAsync(string token, ILogger? logger, string? publicKey = null, string? resource = null);
    }
}

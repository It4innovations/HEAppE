using SshCaAPI.DTO.JsonTypes;

namespace SshCaAPI
{
    public interface ISshCertificateAuthorityService
    {
        public Task<ConfigResponse> GetConfigAsync();
        public Task<SignResponse?> SignAsync(string publicKey, string ott, string resource);
    }
}

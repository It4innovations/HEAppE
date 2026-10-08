using System.Threading.Tasks;

namespace HEAppE.Services.FirecRest;

public interface IFirecRestTokenService
{
    Task<string> GetTokenAsync(string clientId, string clientSecret, string firecRestIdpUrl);
    void InvalidateToken(string clientId, string firecRestIdpUrl);
}

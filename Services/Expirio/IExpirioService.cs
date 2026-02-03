using System.Threading;
using System.Threading.Tasks;
using Services.Expirio.Models;

namespace HEAppE.Services.Expirio;

public interface IExpirioService
{
    /// <summary>
    /// Exchanges an authentication token for a Kerberos ticket using the configured provider.
    /// </summary>
    /// <param name="request">Identifier of Kerberos provider</param>
    /// <param name="token">Token to exchange</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Kerberos ticket string</returns>
    Task<string> ExchangeTokenForKerberosAsync(KerberosExchangeRequest request, string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authentication token.
    /// </summary>
    /// <param name="request">Identifier of client and provider</param>
    /// <param name="token">Token to exchange</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Kerberos ticket string</returns>
    Task<string> ExchangeTokenAsync(ExchangeRequest request, string token, CancellationToken cancellationToken = default);
}

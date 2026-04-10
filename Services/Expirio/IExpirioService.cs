using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    Task<string> ExchangeTokenForKerberosAsync(KerberosExchangeRequest request, string token, ILogger logger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authentication token.
    /// </summary>
    /// <param name="request">Identifier of client and provider</param>
    /// <param name="token">Token to exchange</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Kerberos ticket string</returns>
    Task<string> ExchangeTokenAsync(ExchangeRequest request, string token, ILogger logger, CancellationToken cancellationToken = default);

    Task<bool> ExchangeTokensAsync(string fipToken, string hpcToken, ILogger logger, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the enriched username from a Kerberos ticket exchange response.
    /// </summary>
    /// <param name="token">Token to exchange</param>
    /// <param name="logger"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Enriched username or null</returns>
    Task<string?> GetEnrichedUsernameAsync(string token, ILogger logger, CancellationToken cancellationToken = default);
}

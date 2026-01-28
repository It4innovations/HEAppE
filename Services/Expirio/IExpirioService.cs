using System.Threading;
using System.Threading.Tasks;
using Services.Expirio.Models;

namespace Services.Expirio;

public interface IExpirioService
{
    /// <summary>
    /// Exchanges an authentication token for a Kerberos ticket using the configured provider.
    /// </summary>
    /// <param name="providerName">Identifier of Kerberos provider</param>
    /// <param name="token">Token to exchange</param>
    /// <param name="cancellationToken"></param>
    /// <returns>Kerberos ticket string</returns>
    Task<string> ExchangeTokenForKerberosAsync(KerberosExchangeRequest request, CancellationToken cancellationToken = default);
}

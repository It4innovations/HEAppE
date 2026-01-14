using System.Threading;
using System.Threading.Tasks;

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
    Task<string> ExchangeTokenForKerberosAsync(string providerName, CancellationToken cancellationToken = default);
}

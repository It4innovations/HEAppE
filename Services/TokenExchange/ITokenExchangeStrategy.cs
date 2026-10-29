using System.Threading;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;

namespace HEAppE.Services.TokenExchange;

/// <summary>
/// Strategy interface for performing a specific type of token exchange.
/// </summary>
public interface ITokenExchangeStrategy
{
    /// <summary>
    /// The strategy name that matches TokenExchangeTargetConfiguration.Strategy.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Execute the token exchange.
    /// </summary>
    /// <param name="target">The target configuration.</param>
    /// <param name="subjectToken">The subject token to exchange.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange result.</returns>
    Task<TokenExchangeResult> ExchangeAsync(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        CancellationToken cancellationToken = default);
}

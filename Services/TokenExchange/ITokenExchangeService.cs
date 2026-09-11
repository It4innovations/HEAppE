using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HEAppE.Services.TokenExchange;

/// <summary>
/// Unified service for performing token exchanges across all HEAppE components.
/// Supports named targets with configurable strategies and dependency chaining.
/// </summary>
public interface ITokenExchangeService
{
    /// <summary>
    /// Exchange a token for a specific named target.
    /// </summary>
    /// <param name="targetName">The name of the configured exchange target.</param>
    /// <param name="subjectToken">The subject token to exchange.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exchange result.</returns>
    Task<TokenExchangeResult> ExchangeAsync(
        string targetName,
        string subjectToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute all auto-exchange targets for the given phase, resolving dependencies via topological sort.
    /// Tokens are stored in the provided exchangedTokens dictionary.
    /// </summary>
    /// <param name="phase">The exchange phase ("PreAuth" or "PostAuth").</param>
    /// <param name="incomingToken">The original incoming Bearer token.</param>
    /// <param name="exchangedTokens">Dictionary to store exchanged tokens, keyed by target name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of exchange results keyed by target name.</returns>
    Task<Dictionary<string, TokenExchangeResult>> ExecuteAutoExchangesAsync(
        string phase,
        string incomingToken,
        Dictionary<string, string> exchangedTokens,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all configured target names.
    /// </summary>
    IReadOnlyList<string> GetConfiguredTargets();

    /// <summary>
    /// Check if a specific target is configured and enabled.
    /// </summary>
    bool IsTargetEnabled(string targetName);

    /// <summary>
    /// Check if there are any enabled auto-exchange targets for the specified phase.
    /// </summary>
    bool HasAutoExchangeTargets(string phase);
}

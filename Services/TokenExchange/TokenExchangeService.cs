using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.TokenExchange;

public class TokenExchangeService : ITokenExchangeService
{
    private readonly IReadOnlyDictionary<string, ITokenExchangeStrategy> _strategies;
    private readonly IReadOnlyDictionary<string, TokenExchangeTargetConfiguration> _targets;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenExchangeService> _logger;

    public TokenExchangeService(
        IEnumerable<ITokenExchangeStrategy> strategies,
        IEnumerable<TokenExchangeTargetConfiguration> targets,
        IConfiguration configuration,
        ILogger<TokenExchangeService> logger)
    {
        _strategies = strategies.ToDictionary(s => s.StrategyName, s => s, StringComparer.OrdinalIgnoreCase);
        _targets = targets.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<TokenExchangeResult> ExchangeAsync(
        string targetName,
        string subjectToken,
        CancellationToken cancellationToken = default)
    {
        if (!_targets.TryGetValue(targetName, out var target))
        {
            _logger.LogWarning("Token exchange target '{TargetName}' is not configured.", targetName);
            return TokenExchangeResult.Failed(targetName, $"Target '{targetName}' is not configured.");
        }

        if (!target.IsEnabled)
        {
            _logger.LogInformation("Token exchange target '{TargetName}' is disabled.", targetName);
            return TokenExchangeResult.Failed(targetName, $"Target '{targetName}' is disabled.");
        }

        if (!_strategies.TryGetValue(target.Strategy, out var strategy))
        {
            _logger.LogError("Strategy '{Strategy}' for target '{TargetName}' is not registered.", target.Strategy, targetName);
            return TokenExchangeResult.Failed(targetName, $"Strategy '{target.Strategy}' is not registered.");
        }

        _logger.LogDebug("Executing token exchange for target '{TargetName}' using strategy '{Strategy}'.", targetName, strategy.StrategyName);
        return await strategy.ExchangeAsync(target, subjectToken, cancellationToken);
    }

    public async Task<Dictionary<string, TokenExchangeResult>> ExecuteAutoExchangesAsync(
        string phase,
        string incomingToken,
        Dictionary<string, string> exchangedTokens,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, TokenExchangeResult>(StringComparer.OrdinalIgnoreCase);

        var activeTargets = _targets.Values
            .Where(t => t.IsEnabled && t.AutoExchange && string.Equals(t.AutoExchangePhase, phase, StringComparison.OrdinalIgnoreCase))
            .Where(t => EvaluateCondition(t.AutoExchangeCondition))
            .ToList();

        if (activeTargets.Count == 0)
        {
            return results;
        }

        var sortedTargets = TopologicalSort(activeTargets);

        foreach (var target in sortedTargets)
        {
            string? subjectToken = null;

            if (string.Equals(target.SubjectTokenSource, "incoming", StringComparison.OrdinalIgnoreCase))
            {
                subjectToken = incomingToken;
            }
            else
            {
                if (exchangedTokens.TryGetValue(target.SubjectTokenSource, out var storedToken))
                {
                    subjectToken = storedToken;
                }
                else
                {
                    _logger.LogWarning("Subject token source '{Source}' for target '{Target}' was not found in exchanged tokens. Skipping target.", target.SubjectTokenSource, target.Name);
                    continue;
                }
            }

            if (string.IsNullOrEmpty(subjectToken))
            {
                _logger.LogWarning("Resolved subject token for target '{Target}' is null or empty. Skipping.", target.Name);
                continue;
            }

            var result = await ExchangeAsync(target.Name, subjectToken, cancellationToken);
            results[target.Name] = result;

            if (result.Success && !string.IsNullOrEmpty(result.AccessToken))
            {
                var storeKey = string.IsNullOrEmpty(target.StoreAs) ? target.Name : target.StoreAs;
                exchangedTokens[storeKey] = result.AccessToken;
                _logger.LogDebug("Stored exchanged token for target '{Target}' under key '{StoreKey}'.", target.Name, storeKey);
            }
        }

        return results;
    }

    public IReadOnlyList<string> GetConfiguredTargets()
    {
        return _targets.Keys.ToList();
    }

    public bool IsTargetEnabled(string targetName)
    {
        return _targets.TryGetValue(targetName, out var target) && target.IsEnabled;
    }

    public bool HasAutoExchangeTargets(string phase)
    {
        return _targets.Values.Any(t =>
            t.IsEnabled &&
            t.AutoExchange &&
            string.Equals(t.AutoExchangePhase, phase, StringComparison.OrdinalIgnoreCase) &&
            EvaluateCondition(t.AutoExchangeCondition));
    }

    private bool EvaluateCondition(string? condition)
    {
        if (string.IsNullOrWhiteSpace(condition))
        {
            return true;
        }

        var value = _configuration.GetValue<bool>(condition);
        return value;
    }

    private List<TokenExchangeTargetConfiguration> TopologicalSort(List<TokenExchangeTargetConfiguration> targets)
    {
        var sorted = new List<TokenExchangeTargetConfiguration>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inProcess = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var targetMap = targets.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

        void Visit(TokenExchangeTargetConfiguration target)
        {
            if (visited.Contains(target.Name)) return;

            if (inProcess.Contains(target.Name))
            {
                throw new InvalidOperationException($"Circular dependency detected in token exchange targets involving '{target.Name}'.");
            }

            inProcess.Add(target.Name);

            if (!string.Equals(target.SubjectTokenSource, "incoming", StringComparison.OrdinalIgnoreCase))
            {
                if (targetMap.TryGetValue(target.SubjectTokenSource, out var dependency))
                {
                    Visit(dependency);
                }
            }

            inProcess.Remove(target.Name);
            visited.Add(target.Name);
            sorted.Add(target);
        }

        foreach (var target in targets)
        {
            Visit(target);
        }

        return sorted;
    }
}

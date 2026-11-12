using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.TokenExchange;

public class TokenExchangeService : ITokenExchangeService
{
    private readonly IReadOnlyDictionary<string, ITokenExchangeStrategy> _strategies;
    private readonly IReadOnlyDictionary<string, TokenExchangeTargetConfiguration> _targets;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenExchangeService> _logger;
    private readonly IMemoryCache? _memoryCache;
    private readonly AsyncKeyedLock<string> _keyedLock = new();

    public TokenExchangeService(
        IEnumerable<ITokenExchangeStrategy> strategies,
        IEnumerable<TokenExchangeTargetConfiguration> targets,
        IConfiguration configuration,
        ILogger<TokenExchangeService> logger,
        IMemoryCache? memoryCache = null)
    {
        _strategies = strategies.ToDictionary(s => s.StrategyName, s => s, StringComparer.OrdinalIgnoreCase);
        _targets = targets.ToDictionary(t => t.Name, t => t, StringComparer.OrdinalIgnoreCase);
        _configuration = configuration;
        _logger = logger;
        _memoryCache = memoryCache;
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

        string? cacheKey = null;
        if (target.EnableCaching && _memoryCache != null)
        {
            cacheKey = BuildCacheKey(target, subjectToken);

            if (_memoryCache.TryGetValue(cacheKey, out TokenExchangeResult? cachedResult) && cachedResult != null)
            {
                _logger.LogDebug("[TokenExchange] Cache hit for target '{TargetName}'.", targetName);
                return cachedResult.Clone();
            }
        }

        if (cacheKey != null)
        {
            using (await _keyedLock.LockAsync(cacheKey, cancellationToken).ConfigureAwait(false))
            {
                // Double-checked locking: check cache again in case another concurrent request completed the exchange while we waited
                if (_memoryCache != null && _memoryCache.TryGetValue(cacheKey, out TokenExchangeResult? cachedResult) && cachedResult != null)
                {
                    _logger.LogDebug("[TokenExchange] Cache hit after lock for target '{TargetName}'.", targetName);
                    return cachedResult.Clone();
                }

                return await ExecuteExchangeInternalAsync(target, subjectToken, cacheKey, cancellationToken).ConfigureAwait(false);
            }
        }

        return await ExecuteExchangeInternalAsync(target, subjectToken, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TokenExchangeResult> ExecuteExchangeInternalAsync(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        string? cacheKey,
        CancellationToken cancellationToken)
    {
        string targetName = target.Name;
        if (!_strategies.TryGetValue(target.Strategy, out var strategy))
        {
            _logger.LogError("Strategy '{Strategy}' for target '{TargetName}' is not registered.", target.Strategy, targetName);
            return TokenExchangeResult.Failed(targetName, $"Strategy '{target.Strategy}' is not registered.");
        }

        if (target.IntrospectSubjectToken &&
            !string.Equals(target.Strategy, "Introspection", StringComparison.OrdinalIgnoreCase))
        {
            if (!_strategies.TryGetValue("Introspection", out var introspectionStrategy))
            {
                _logger.LogError("Strategy 'Introspection' is not registered, cannot introspect subject token for target '{TargetName}'.", targetName);
                return TokenExchangeResult.Failed(targetName, "Strategy 'Introspection' is not registered.");
            }

            _logger.LogDebug("Introspecting subject token before exchange for target '{TargetName}'.", targetName);
            var introspectionResult = await introspectionStrategy.ExchangeAsync(target, subjectToken, cancellationToken);
            if (!introspectionResult.Success)
            {
                _logger.LogWarning("Subject token introspection failed for target '{TargetName}': {Error}", targetName, introspectionResult.Error);
                return TokenExchangeResult.Failed(targetName, $"Subject token introspection failed: {introspectionResult.Error}");
            }
        }

        _logger.LogDebug("Executing token exchange for target '{TargetName}' using strategy '{Strategy}'.", targetName, strategy.StrategyName);
        var result = await strategy.ExchangeAsync(target, subjectToken, cancellationToken);

        if (result.Success && !string.IsNullOrEmpty(result.AccessToken) && target.EnableCaching && _memoryCache != null && cacheKey != null)
        {
            var ttl = CalculateCacheTtl(target, subjectToken, result);
            if (ttl > TimeSpan.FromSeconds(5))
            {
                _memoryCache.Set(cacheKey, result.Clone(), ttl);
                _logger.LogDebug("[TokenExchange] Cached exchanged token for target '{TargetName}' for {TtlSeconds}s.", targetName, (int)ttl.TotalSeconds);
            }
        }

        return result;
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

    public bool HasPreIntrospectionConfigured(string phase)
    {
        return _targets.Values.Any(t =>
            t.IsEnabled &&
            t.AutoExchange &&
            string.Equals(t.AutoExchangePhase, phase, StringComparison.OrdinalIgnoreCase) &&
            EvaluateCondition(t.AutoExchangeCondition) &&
            (t.IntrospectSubjectToken || string.Equals(t.Strategy, "Introspection", StringComparison.OrdinalIgnoreCase)));
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

    public void InvalidateCache(string targetName, string subjectToken)
    {
        if (_memoryCache != null)
        {
            var userId = GetUserId(subjectToken);
            if (!string.IsNullOrEmpty(userId))
            {
                _memoryCache.Remove($"token_exchange:{targetName}:user:{userId}");
            }

            string tokenHash = !string.IsNullOrEmpty(subjectToken) ? ComputeSha256(subjectToken) : "no_token";
            _memoryCache.Remove($"token_exchange:{targetName}:token:{tokenHash}");
            _memoryCache.Remove($"token_exchange:{targetName}:{tokenHash}");

            _logger.LogDebug("[TokenExchange] Evicted cache for target '{TargetName}'.", targetName);
        }
    }

    private string BuildCacheKey(TokenExchangeTargetConfiguration target, string subjectToken)
    {
        if (target.CacheByUser)
        {
            var userId = GetUserId(subjectToken);
            if (!string.IsNullOrEmpty(userId))
            {
                return $"token_exchange:{target.Name}:user:{userId}";
            }
        }

        string tokenHash = !string.IsNullOrEmpty(subjectToken) ? ComputeSha256(subjectToken) : "no_token";
        return $"token_exchange:{target.Name}:token:{tokenHash}";
    }

    private TimeSpan CalculateCacheTtl(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        TokenExchangeResult result)
    {
        if (target.CacheDurationSeconds.HasValue && target.CacheDurationSeconds.Value > 0)
        {
            return TimeSpan.FromSeconds(target.CacheDurationSeconds.Value);
        }

        DateTime? exchangedExp = null;
        if (result.ExpiresIn > 0)
        {
            exchangedExp = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
        }
        if (!exchangedExp.HasValue && !string.IsNullOrEmpty(result.AccessToken))
        {
            exchangedExp = GetTokenExpiration(result.AccessToken);
        }

        DateTime? subjectExp = GetTokenExpiration(subjectToken);

        DateTime expirationTime;
        if (target.CacheByUser && exchangedExp.HasValue)
        {
            // When caching per user identity, the exchanged token is valid until the exchanged token itself expires.
            // Any incoming request is already validated by the authentication middleware before reaching token exchange.
            expirationTime = exchangedExp.Value;
        }
        else if (subjectExp.HasValue && exchangedExp.HasValue)
        {
            expirationTime = subjectExp.Value < exchangedExp.Value ? subjectExp.Value : exchangedExp.Value;
        }
        else if (exchangedExp.HasValue)
        {
            expirationTime = exchangedExp.Value;
        }
        else if (subjectExp.HasValue)
        {
            expirationTime = subjectExp.Value;
        }
        else
        {
            expirationTime = DateTime.UtcNow.AddMinutes(10);
        }

        return CacheUtils.CalculateAdaptiveTtl(expirationTime);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static string? GetUserId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var cleanToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token.Substring(7).Trim()
                : token.Trim();

            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(cleanToken))
            {
                var jwt = handler.ReadJwtToken(cleanToken);
                var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                if (!string.IsNullOrEmpty(sub)) return sub;
            }
        }
        catch
        {
            // Ignore parsing exceptions for opaque or non-JWT tokens
        }
        return null;
    }

    private static DateTime? GetTokenExpiration(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var cleanToken = token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? token.Substring(7).Trim()
                : token.Trim();

            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(cleanToken))
            {
                var jwt = handler.ReadJwtToken(cleanToken);
                if (jwt.ValidTo != DateTime.MinValue && jwt.ValidTo != DateTime.MaxValue)
                {
                    return jwt.ValidTo;
                }
            }
        }
        catch
        {
            // ignored
        }
        return null;
    }
}

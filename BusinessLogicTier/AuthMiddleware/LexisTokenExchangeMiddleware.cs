using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.AuthMiddleware;
using HEAppE.Services.Expirio;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HEAppE.Services.Expirio.Configuration;
using HEAppE.Services.Expirio.Models;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

public class LexisTokenExchangeMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public LexisTokenExchangeMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger("HEAppE.BusinessLogicTier.AuthMiddleware.LexisTokenExchangeMiddleware");
    }

    public async Task InvokeAsync(HttpContext context, ILexisTokenService lexisTokenService,
        IExpirioService expirioService, IMemoryCache? memoryCache = null)
    {
        ApplyRequestSizeLimit(context);

        bool isBearer = context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                        authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (isBearer)
        {
            var incomingToken = authHeader.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices.GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;

            if ((LexisAuthenticationConfiguration.UseBearerAuth || JwtTokenIntrospectionConfiguration.IsEnabled) &&
                !JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled)
            {
                if ((LexisAuthenticationConfiguration.UseBearerAuth && JwtTokenIntrospectionConfiguration.IsEnabled) ||
                    JwtTokenIntrospectionConfiguration.IsEnabled)
                {
                    _logger.LogInformation(
                        $"LexisTokenExchangeMiddleware: Introspection enabled but Lexis token exchange flow disabled. Using incoming token as IdP token.");
                    context.Request.Headers["Authorization"] = $"Bearer {incomingToken}";
                    contextKeysService.Context.IdpToken = incomingToken;
                    _logger.LogDebug($"LexisTokenExchangeMiddleware: IdP Token set to incoming token: {HEAppE.Utils.StringUtils.MaskToken(incomingToken)}");
                }
                else if (LexisAuthenticationConfiguration.UseBearerAuth &&
                         !JwtTokenIntrospectionConfiguration.IsEnabled)
                {
                    _logger.LogInformation(
                        $"LexisTokenExchangeMiddleware: Bearer auth enabled but introspection disabled. Using incoming token.");
                }
            }
            else if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled)
            {
                _logger.LogInformation("LexisTokenExchangeMiddleware: Exchanging LEXIS token for IdP token");
                try
                {
                    string providerKey = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.UseExpirioServiceForTokenExchange
                        ? $"expirio:{ExpirioSettings.ProviderName}"
                        : $"keycloak:{JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.Broker}";
                    string tokenHash = ComputeSha256(incomingToken);
                    string cacheKey = $"fip_token_exchange:{providerKey}:{tokenHash}";

                    string exchanged;
                    if (memoryCache != null && memoryCache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrEmpty(cached))
                    {
                        _logger.LogDebug("LexisTokenExchangeMiddleware: Token exchange cache hit.");
                        exchanged = cached;
                    }
                    else
                    {
                        if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration
                            .UseExpirioServiceForTokenExchange)
                        {
                            _logger.LogInformation(
                                $"LexisTokenExchangeMiddleware: Using Expirio (Provider: {ExpirioSettings.ProviderName})");
                            var request = new ExchangeRequest()
                            {
                                ProviderName = ExpirioSettings.ProviderName,
                                ClientName = JwtTokenIntrospectionConfiguration.ClientId
                            };
                            exchanged = await expirioService.ExchangeTokenAsync(request, incomingToken, _logger);
                        }
                        else
                        {
                            _logger.LogInformation("LexisTokenExchangeMiddleware: Using LexisTokenService");
                            exchanged = await lexisTokenService.ExchangeLexisTokenForIdpAsync(incomingToken);
                        }

                        if (memoryCache != null && !string.IsNullOrEmpty(exchanged))
                        {
                            var incomingExp = GetTokenExpiration(incomingToken);
                            var exchangedExp = GetTokenExpiration(exchanged);

                            DateTime expirationTime;
                            if (incomingExp.HasValue && exchangedExp.HasValue)
                                expirationTime = incomingExp.Value < exchangedExp.Value ? incomingExp.Value : exchangedExp.Value;
                            else if (incomingExp.HasValue)
                                expirationTime = incomingExp.Value;
                            else if (exchangedExp.HasValue)
                                expirationTime = exchangedExp.Value;
                            else
                                expirationTime = DateTime.UtcNow.AddMinutes(10);

                            var ttl = HEAppE.Utils.CacheUtils.CalculateAdaptiveTtl(expirationTime);
                            if (ttl > TimeSpan.FromSeconds(5))
                            {
                                memoryCache.Set(cacheKey, exchanged, ttl);
                                _logger.LogDebug("LexisTokenExchangeMiddleware: Cached exchanged token for {TtlSeconds}s.", (int)ttl.TotalSeconds);
                            }
                        }
                    }

                    context.Request.Headers["Authorization"] = $"Bearer {exchanged}";
                    contextKeysService.Context.IdpToken = exchanged;
                    _logger.LogDebug($"LexisTokenExchangeMiddleware: Success. IdP Token: {HEAppE.Utils.StringUtils.MaskToken(exchanged)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"LexisTokenExchangeMiddleware: Exchange failed: {ex.Message}");

                    var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                    {
                        Status = StatusCodes.Status401Unauthorized,
                        Title = "Token Exchange Failed",
                        Detail = ex.Message
                    };

                    if (ex is HEAppE.Exceptions.External.AuthenticationTypeException authEx)
                    {
                        problem.Title = authEx.ServiceName != null ? $"Token Exchange Failed ({authEx.ServiceName})" : "Token Exchange Failed";
                        problem.Detail = authEx.Message + (authEx.Details != null ? $": {authEx.Details}" : "");
                    }
                    else if (ex is HEAppE.Exceptions.AbstractTypes.ExternalException externalEx)
                    {
                        problem.Status = StatusCodes.Status502BadGateway;
                        problem.Title = !string.IsNullOrEmpty(externalEx.ServiceName) ? $"Token Exchange External Problem ({externalEx.ServiceName})" : "Token Exchange External Problem";
                        problem.Detail = externalEx.Message + (externalEx.Details != null ? $": {externalEx.Details}" : "");
                    }

                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = problem.Status.Value;
                    await context.Response.WriteAsJsonAsync(problem);
                    return;
                }
            }
        }
        await _next(context);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static DateTime? GetTokenExpiration(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
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

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, (bool isSizeLimit, bool isAllowLargeBody)> _sizeLimitMetadataTypeCache = new();
    private static volatile System.Reflection.PropertyInfo? _featureIsReadOnlyProp;
    private static volatile System.Reflection.PropertyInfo? _featureMaxRequestBodySizeProp;
    private static volatile System.Reflection.PropertyInfo? _sizeLimitMaxRequestBodySizeProp;

    private static void ApplyRequestSizeLimit(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return;

        object? sizeLimitMetadata = null;
        object? allowLargeBodyMetadata = null;

        foreach (var m in endpoint.Metadata)
        {
            var mType = m.GetType();
            if (!_sizeLimitMetadataTypeCache.TryGetValue(mType, out var flags))
            {
                flags = (mType.GetInterface("IRequestSizeLimitMetadata") != null,
                         mType.GetInterface("IAllowLargeRequestBodyMetadata") != null);
                _sizeLimitMetadataTypeCache[mType] = flags;
            }

            if (flags.isAllowLargeBody) allowLargeBodyMetadata = m;
            if (flags.isSizeLimit) sizeLimitMetadata = m;
        }

        if (sizeLimitMetadata == null && allowLargeBodyMetadata == null) return;

        var feature = context.Features.FirstOrDefault(f => f.Key.Name == "IHttpRequestBodySizeFeature").Value;
        if (feature == null) return;

        var featureType = feature.GetType();
        var isReadOnlyProp = _featureIsReadOnlyProp ??= featureType.GetProperty("IsReadOnly");
        if (isReadOnlyProp != null && (bool)isReadOnlyProp.GetValue(feature)!) return;

        var maxRequestBodySizeProp = _featureMaxRequestBodySizeProp ??= featureType.GetProperty("MaxRequestBodySize");
        if (maxRequestBodySizeProp == null) return;

        if (allowLargeBodyMetadata != null)
        {
            maxRequestBodySizeProp.SetValue(feature, null);
        }
        else if (sizeLimitMetadata != null)
        {
            var limitProp = _sizeLimitMaxRequestBodySizeProp ??= sizeLimitMetadata.GetType().GetProperty("MaxRequestBodySize");
            if (limitProp != null)
            {
                maxRequestBodySizeProp.SetValue(feature, limitProp.GetValue(sizeLimitMetadata));
            }
        }
    }
}
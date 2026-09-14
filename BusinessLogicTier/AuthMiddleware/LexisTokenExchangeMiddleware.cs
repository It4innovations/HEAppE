using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.AuthMiddleware;
using HEAppE.Services.Expirio;
using HEAppE.Services.TokenExchange;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HEAppE.Services.Expirio.Configuration;
using HEAppE.Services.Expirio.Models;

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
        IExpirioService expirioService, ITokenExchangeService tokenExchangeService)
    {
        ApplyRequestSizeLimit(context);

        bool isBearer = context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                        authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (isBearer)
        {
            var incomingToken = authHeader.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices.GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;

            // === New unified token exchange: PreAuth phase ===
            if (tokenExchangeService.HasAutoExchangeTargets("PreAuth"))
            {
                _logger.LogInformation("LexisTokenExchangeMiddleware: Using unified token exchange (PreAuth phase).");
                var results = await tokenExchangeService.ExecuteAutoExchangesAsync(
                    "PreAuth", incomingToken, contextKeysService.Context.ExchangedTokens);

                // If a PreAuth exchange produced an IdP or FIP token, update the Authorization header
                var idpToken = contextKeysService.Context.GetExchangedToken("idp") ?? contextKeysService.Context.GetExchangedToken("fip");
                if (!string.IsNullOrEmpty(idpToken))
                {
                    context.Request.Headers["Authorization"] = $"Bearer {idpToken}";
                    contextKeysService.Context.IdpToken = idpToken;
                    _logger.LogDebug("LexisTokenExchangeMiddleware: Authorization header updated with exchanged IdP/FIP token.");
                }
                else if ((LexisAuthenticationConfiguration.UseBearerAuth || JwtTokenIntrospectionConfiguration.IsEnabled) &&
                         !JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled)
                {
                    // No IdP exchange configured, use incoming token as-is
                    contextKeysService.Context.IdpToken = incomingToken;
                }
            }
            // === Legacy fallback (when no TokenExchangeTargets configured) ===
            else if ((LexisAuthenticationConfiguration.UseBearerAuth || JwtTokenIntrospectionConfiguration.IsEnabled) &&
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
                _logger.LogInformation("LexisTokenExchangeMiddleware: Exchanging LEXIS token for IdP token (legacy)");
                try
                {
                    string exchanged;
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
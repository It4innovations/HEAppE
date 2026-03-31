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
using Services.Expirio.Configuration;
using Services.Expirio.Models;

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

    public async Task InvokeAsync(HttpContext context, ILexisTokenService lexisTokenService, IExpirioService expirioService)
    {
        ApplyRequestSizeLimit(context);
        context.Request.EnableBuffering();
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
        {
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            _logger.LogDebug($"[HEAppE Request] Path: {context.Request.Path}, Body: {body}");
        }

        bool isBearer = context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
                        authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);

        if (isBearer)
        {
            var incomingToken = authHeader.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices.GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;

            if ((LexisAuthenticationConfiguration.UseBearerAuth || JwtTokenIntrospectionConfiguration.IsEnabled) && !JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled)
            {
                if ((LexisAuthenticationConfiguration.UseBearerAuth && JwtTokenIntrospectionConfiguration.IsEnabled) || JwtTokenIntrospectionConfiguration.IsEnabled)
                {
                    _logger.LogInformation($"LexisTokenExchangeMiddleware: Introspection enabled but Lexis token exchange flow disabled. Using incoming token as FIP token.");
                    context.Request.Headers["Authorization"] = $"Bearer {incomingToken}";
                    contextKeysService.Context.FIPToken = incomingToken;
                    _logger.LogDebug($"LexisTokenExchangeMiddleware: FIP Token set to incoming token: {incomingToken}");
                }
                else if (LexisAuthenticationConfiguration.UseBearerAuth && !JwtTokenIntrospectionConfiguration.IsEnabled)
                {
                    _logger.LogInformation($"LexisTokenExchangeMiddleware: Bearer auth enabled but introspection disabled. Using incoming token.");
                }
            }
            else if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled)
            {
                _logger.LogInformation("LexisTokenExchangeMiddleware: Exchanging LEXIS token for FIP token");
                try
                {
                    string exchanged;
                    if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.UseExpirioServiceForTokenExchange)
                    {
                        _logger.LogInformation($"LexisTokenExchangeMiddleware: Using Expirio (Provider: {ExpirioSettings.ProviderName})");
                        var request = new ExchangeRequest()
                        {
                            ProviderName = ExpirioSettings.ProviderName,
                            ClientName = JwtTokenIntrospectionConfiguration.ClientId
                        };
                        exchanged = await expirioService.ExchangeTokenAsync(request, incomingToken);
                    }
                    else
                    {
                        _logger.LogInformation("LexisTokenExchangeMiddleware: Using LexisTokenService");
                        exchanged = await lexisTokenService.ExchangeLexisTokenForFipAsync(incomingToken);
                    }

                    context.Request.Headers["Authorization"] = $"Bearer {exchanged}";
                    contextKeysService.Context.FIPToken = exchanged;
                    _logger.LogDebug($"LexisTokenExchangeMiddleware: Success. FIP Token: {exchanged}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"LexisTokenExchangeMiddleware: Exchange failed: {ex.Message}", ex);
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Token exchange failed");
                    return;
                }
            }
        }
        
        bool isStreamingEndpoint = context.Request.Path.Value
            ?.Contains("HttpPostToJobNodeStream", StringComparison.OrdinalIgnoreCase) == true;

        if (isStreamingEndpoint)
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();
        _logger.LogDebug($"[HEAppE Response] Path: {context.Request.Path}, Status: {context.Response.StatusCode}, Body: {responseText}");
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        responseBody.Seek(0, SeekOrigin.Begin);
        await responseBody.CopyToAsync(originalBodyStream);
    }

    private static void ApplyRequestSizeLimit(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null) return;

        // Use reflection to avoid compile-time dependency on Metadata/Features interfaces in this library
        var metadata = endpoint.Metadata;
        var sizeLimitMetadata = metadata.FirstOrDefault(m => m.GetType().GetInterface("IRequestSizeLimitMetadata") != null);
        var allowLargeBodyMetadata = metadata.FirstOrDefault(m => m.GetType().GetInterface("IAllowLargeRequestBodyMetadata") != null);

        if (sizeLimitMetadata == null && allowLargeBodyMetadata == null) return;

        var feature = context.Features.FirstOrDefault(f => f.Key.Name == "IHttpRequestBodySizeFeature").Value;
        if (feature == null) return;

        var isReadOnlyProp = feature.GetType().GetProperty("IsReadOnly");
        if (isReadOnlyProp != null && (bool)isReadOnlyProp.GetValue(feature)) return;

        var maxRequestBodySizeProp = feature.GetType().GetProperty("MaxRequestBodySize");
        if (maxRequestBodySizeProp == null) return;

        if (allowLargeBodyMetadata != null)
        {
            maxRequestBodySizeProp.SetValue(feature, null);
        }
        else if (sizeLimitMetadata != null)
        {
            var limitProp = sizeLimitMetadata.GetType().GetProperty("MaxRequestBodySize");
            if (limitProp != null)
            {
                maxRequestBodySizeProp.SetValue(feature, limitProp.GetValue(sizeLimitMetadata));
            }
        }
    }
}
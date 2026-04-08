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

#if DEBUG
        // TODO: remove, do not commit!
        context.RequestServices.GetRequiredService<IHttpContextKeys>().Context.FIPToken = """eyJhbGciOiJSUzI1NiIsInR5cCIgOiAiSldUIiwia2lkIiA6ICJpWmV2WHI1QmJNbDNyd2daVi1WdDVNbjhobUozVlNJaUh1Y1lITGFYcHRNIn0.eyJleHAiOjE3NzU2NzM3NjAsImlhdCI6MTc3NTY1MjE2MCwiYXV0aF90aW1lIjoxNzc1NjUyMTU3LCJqdGkiOiJvbnJ0ZGc6YjBkOWYyMTUtZjBkYS0xOWYzLWY4NzMtNmM1ODA5MWNkYWUzIiwiaXNzIjoiaHR0cHM6Ly9hYWkuZGV2LmxleGlzLnRlY2gvYXV0aC9yZWFsbXMvTEVYSVNfQUFJX3YyX0RFViIsImF1ZCI6WyJoZWFwcGUiLCJwb3J0YWwiLCJkZGkiLCJhaXJmbG93IiwiZGRpMi1wb3J0YWwiLCJicm9rZXIiLCJhY2NvdW50Il0sInN1YiI6IjIzMTZhYTJjLWE2YWQtNDA4My1hMDUwLTQ1YWVhZDhjYjFlZiIsInR5cCI6IkJlYXJlciIsImF6cCI6IkxFWElTX1BZNExFWElTX0NMSSIsInNpZCI6Im1nR1pXMWxUMVpDRU5HOTdZNmh3czU5YyIsImFjciI6IjEiLCJhbGxvd2VkLW9yaWdpbnMiOlsiLyoiXSwicmVhbG1fYWNjZXNzIjp7InJvbGVzIjpbIm9mZmxpbmVfYWNjZXNzIiwiZGVmYXVsdC1yb2xlcy1sZXhpc19hYWlfdjIiLCJ1bWFfYXV0aG9yaXphdGlvbiJdfSwicmVzb3VyY2VfYWNjZXNzIjp7ImJyb2tlciI6eyJyb2xlcyI6WyJyZWFkLXRva2VuIl19LCJhY2NvdW50Ijp7InJvbGVzIjpbIm1hbmFnZS1hY2NvdW50IiwibWFuYWdlLWFjY291bnQtbGlua3MiLCJ2aWV3LXByb2ZpbGUiXX19LCJzY29wZSI6Im9wZW5pZCBhdWRpZW5jZSBlbWFpbCBwcm9maWxlIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsIm5hbWUiOiJKYW4gVHJ1cGwiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiIzNjFhNTE0NC05ZWI5LTRmNTgtYmY5Ny04MTRhMDAxMTIxYWFAbGV4aXMudGVjaCIsImdpdmVuX25hbWUiOiJKYW4iLCJmYW1pbHlfbmFtZSI6IlRydXBsIiwiZW1haWwiOiJqYW4udHJ1cGxAdnNiLmN6In0.LREf7vDetCR7-9ioAGmVbklbqxLtx1SPsOHpSKFksvXTBwCurcp80NPLcmPGi8MUHKjSVY5iLDKj4mk3FhpS-xQJq93Da1oser9sRWv2l-7w0Bc7Ym3SbliQ5VqofYRYVp3XteDgJ6s7AWaQ1YYBkPBkU-i2pZjpAdXqQ3DFQ-7ylazHAD7kAud7RPMQz0ufLhM31Tx7DBVuipDdlDZ6hX8JKISLh1HFKp2f9Kve8lyKHsQkVu7lN1GIAwX36JZeoZ8DbeHSUDQ5zS-O3epQIdwKoNejNMI2m_e29G2Z_fOQL4R2Nnmo1QBoxAQrhbOb7tJkiphnhd4w3oqivLj6bA""";
#endif

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
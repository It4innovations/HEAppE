using System;
using System.IO;
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
        context.Request.EnableBuffering();
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
        {
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
            _logger.LogDebug($"[Exchange Request] Path: {context.Request.Path}, Body: {body}");
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
                        exchanged = await expirioService.ExchangeTokenAsync(request, incomingToken, _logger);
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

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        _logger.LogDebug($"[Exchange Response] Path: {context.Request.Path}, Status: {context.Response.StatusCode}, Body: {responseText}");

        await responseBody.CopyToAsync(originalBodyStream);
    }
}
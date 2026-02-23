using System;
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
        ILogger logger = _logger;
        if ((LexisAuthenticationConfiguration.UseBearerAuth && 
            !JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled) &&
        context.Request.Headers.TryGetValue("Authorization", out var authHeaderLexis) &&
        authHeaderLexis.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("LexisTokenExchangeMiddleware: Extracting LEXIS token from Authorization header"); 
            var incomingToken = authHeaderLexis.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices
                .GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;
        }
        else if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled &&
            context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            logger?.LogInformation("LexisTokenExchangeMiddleware: Exchanging LEXIS token for FIP token");
            var incomingToken = authHeader.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices
                .GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;
            try
            {
                if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.UseExpirioServiceForTokenExchange)
                {
                    logger?.LogInformation("LexisTokenExchangeMiddleware: Using Expirio service for token exchange");
                    ExchangeRequest request = new ExchangeRequest()
                    {
                        ProviderName = ExpirioSettings.ProviderName,
                        ClientName = JwtTokenIntrospectionConfiguration.ClientId

                    };
                    var exchanged = await expirioService.ExchangeTokenAsync(request, incomingToken);
                    context.Request.Headers["Authorization"] = $"Bearer {exchanged}";
                    contextKeysService.Context.FIPToken = exchanged;
                }
                else
                {
                    logger?.LogInformation("LexisTokenExchangeMiddleware: Using LexisTokenService for token exchange");
                    var exchanged = await lexisTokenService.ExchangeLexisTokenForFipAsync(incomingToken);
                    context.Request.Headers["Authorization"] = $"Bearer {exchanged}";
                    contextKeysService.Context.FIPToken = exchanged;
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token exchange failed: {ex.Message}");
            }
        }

        await _next(context);
    }
}

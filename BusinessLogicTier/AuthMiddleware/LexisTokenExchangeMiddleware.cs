using System;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.AuthMiddleware;
using HEAppE.Services.Expirio;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Services.Expirio.Configuration;
using Services.Expirio.Models;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;


public class LexisTokenExchangeMiddleware
{
    private readonly RequestDelegate _next;

    public LexisTokenExchangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILexisTokenService lexisTokenService, IExpirioService expirioService)
    {
        var log = LogManager.GetLogger(typeof(LexisTokenExchangeMiddleware));
        if ((LexisAuthenticationConfiguration.UseBearerAuth && 
            !JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled) &&
        context.Request.Headers.TryGetValue("Authorization", out var authHeaderLexis) &&
        authHeaderLexis.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            log.Info("LexisTokenExchangeMiddleware: Extracting LEXIS token from Authorization header"); 
            var incomingToken = authHeaderLexis.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices
                .GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;
        }
        else if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled &&
            context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            log.Info("LexisTokenExchangeMiddleware: Exchanging LEXIS token for FIP token");
            var incomingToken = authHeader.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices
                .GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;
            try
            {
                if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.UseExpirioServiceForTokenExchange)
                {
                    log.Info("LexisTokenExchangeMiddleware: Using Expirio service for token exchange");
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
                    log.Info("LexisTokenExchangeMiddleware: Using LexisTokenService for token exchange");
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

using System;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HEAppE.BusinessLogicTier;


public class LexisTokenExchangeMiddleware
{
    private readonly RequestDelegate _next;

    public LexisTokenExchangeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILexisTokenService lexisTokenService)
    {
        if ((LexisAuthenticationConfiguration.UseBearerAuth && 
            !JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled) &&
        context.Request.Headers.TryGetValue("Authorization", out var authHeaderLexis) &&
        authHeaderLexis.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var incomingToken = authHeaderLexis.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices
                .GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;
            
        }
        else if (JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled &&
            context.Request.Headers.TryGetValue("Authorization", out var authHeader) &&
            authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var incomingToken = authHeader.ToString()["Bearer ".Length..].Trim();
            var contextKeysService = context.RequestServices
                .GetRequiredService<IHttpContextKeys>();
            contextKeysService.Context.LEXISToken = incomingToken;
            try
            {
                var exchanged = await lexisTokenService.ExchangeLexisTokenForFipAsync(incomingToken);
                context.Request.Headers["Authorization"] = $"Bearer {exchanged}";
                contextKeysService.Context.FIPToken = exchanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token exchange failed: {ex.Message}");
            }
        }

        await _next(context);
    }
}

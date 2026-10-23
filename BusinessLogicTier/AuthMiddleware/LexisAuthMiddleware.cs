using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using SshCaAPI;
using HEAppE.Services.Expirio;
using HEAppE.Services.TokenExchange;
using Microsoft.Extensions.DependencyInjection;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

public class LexisAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public LexisAuthMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
    {
        _next = next;
        _logger = loggerFactory.CreateLogger("HEAppE.BusinessLogicTier.AuthMiddleware.LexisAuthMiddleware");
    }

    public async Task InvokeAsync(HttpContext context, IHttpContextKeys keys, ISshCertificateAuthorityService sshCaService, IUserOrgService userOrgService, IExpirioService expirioService)
    {
        if (context.Items.ContainsKey("AuthErrorHandled"))
        {
            return;
        }

        // check if the endpoint allows anonymous access
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            _logger.LogInformation("AuthMiddleware: Anonymous endpoint detected.");
            await _next(context);
            return;
        }

        if (context.Items.ContainsKey("AuthErrorHandled"))
        {
            return;
        }

        string authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        _logger.LogInformation($"Auth Check - UseBearer: {LexisAuthenticationConfiguration.UseBearerAuth}, IntrospectionEnabled: {JwtTokenIntrospectionConfiguration.IsEnabled}, HeaderPresent: {authHeader != null}");

        bool isBearer = authHeader?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true;

        if (JwtTokenIntrospectionConfiguration.IsEnabled && isBearer)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                string errorDetail = context.Items.TryGetValue("IntrospectionError", out var errObj) && errObj is string errStr && !string.IsNullOrEmpty(errStr)
                    ? $"Authentication failed (Keycloak): {errStr}"
                    : "Token introspection failed: token is invalid or not active.";

                _logger.LogWarning("AuthMiddleware: OAuth2 introspection failed for Bearer token.");

                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized Access (Keycloak)",
                    Detail = errorDetail
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = problem.Status.Value;
                await context.Response.WriteAsJsonAsync(problem);
                return;
            }

            _logger.LogInformation("AuthMiddleware: Bearer token validated and authorized via OAuth2Introspection.");
            HEAppE.Utils.LoggingUtils.AddUserPropertiesToLogThreadContext(
                keys.Context.AdaptorUserId, keys.Context.UserName, keys.Context.Email);
        }
        else if (LexisAuthenticationConfiguration.UseBearerAuth && isBearer)
        {
            _logger.LogInformation("AuthMiddleware: Processing Bearer token.");
            string token = authHeader["Bearer ".Length..].Trim();
            if (string.IsNullOrEmpty(keys.Context.LEXISToken))
            {
                keys.Context.LEXISToken = token;
            }
            
            try
            {
                var tokenExchangeService = context.RequestServices.GetService<ITokenExchangeService>();
                if (tokenExchangeService != null && tokenExchangeService.HasAutoExchangeTargets("PostAuth"))
                {
                    if (!string.IsNullOrEmpty(keys.Context.IdpToken) &&
                        !keys.Context.ExchangedTokens.ContainsKey("idp"))
                    {
                        keys.Context.ExchangedTokens["idp"] = keys.Context.IdpToken;
                    }

                    await tokenExchangeService.ExecuteAutoExchangesAsync(
                        "PostAuth",
                        keys.Context.LEXISToken ?? keys.Context.IdpToken ?? token,
                        keys.Context.ExchangedTokens);
                }

                await keys.Authorize(sshCaService, userOrgService, expirioService);
                var identity = new ClaimsIdentity(new[] { new Claim("raw_token", token) }, "Lexis");
                context.User = new ClaimsPrincipal(identity);
                _logger.LogInformation("AuthMiddleware: Internal Authorize success.");

                // Add to LogContext early so Auth logs also get the user
                HEAppE.Utils.LoggingUtils.AddUserPropertiesToLogThreadContext(
                    keys.Context.AdaptorUserId, keys.Context.UserName, keys.Context.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"AuthMiddleware: Internal Authorize failed: {ex.Message}");

                var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred during authorization."
                };

                if (ex is HEAppE.Exceptions.External.AuthenticationTypeException authEx)
                {
                    problem.Status = StatusCodes.Status401Unauthorized;
                    problem.Title = authEx.ServiceName != null ? $"Unauthorized Access ({authEx.ServiceName})" : "Unauthorized Access";
                    problem.Detail = authEx.Message + (authEx.Details != null ? $": {authEx.Details}" : "");
                }
                else if (ex is HEAppE.Exceptions.AbstractTypes.ExternalException externalEx)
                {
                    problem.Status = StatusCodes.Status502BadGateway;
                    problem.Title = !string.IsNullOrEmpty(externalEx.ServiceName) ? $"External Problem ({externalEx.ServiceName})" : "External Problem";
                    problem.Detail = externalEx.Message + (externalEx.Details != null ? $": {externalEx.Details}" : "");
                }
                else if (ex is UnauthorizedAccessException)
                {
                    problem.Status = StatusCodes.Status401Unauthorized;
                    problem.Title = "Unauthorized Access";
                    problem.Detail = ex.Message;
                }

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = problem.Status.Value;
                await context.Response.WriteAsJsonAsync(problem);
                return;
            }
        }
        else if (context.User?.Identity?.IsAuthenticated != true && !context.Request.Headers.ContainsKey("X-API-Key"))
        {
            _logger.LogInformation("AuthMiddleware: Falling back to LocalScheme.");
            var identity = new ClaimsIdentity(new[] { new Claim("raw_token", string.Empty) }, "LocalScheme");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
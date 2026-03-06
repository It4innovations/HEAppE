using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

public class LexisAuthMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ILog Log = LogManager.GetLogger(typeof(LexisAuthMiddleware));

    public LexisAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IHttpContextKeys keys, ISshCertificateAuthorityService sshCaService, IUserOrgService userOrgService)
    {
        Log.Info($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}");

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            Log.Info("AuthMiddleware: Anonymous endpoint detected.");
            await _next(context);
            return;
        }

        string authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        Log.Info($"Auth Check - UseBearer: {LexisAuthenticationConfiguration.UseBearerAuth}, IntrospectionEnabled: {JwtTokenIntrospectionConfiguration.IsEnabled}, HeaderPresent: {authHeader != null}");

        if ((LexisAuthenticationConfiguration.UseBearerAuth || JwtTokenIntrospectionConfiguration.IsEnabled) && authHeader?.StartsWith("Bearer ") == true)
        {
            Log.Info("AuthMiddleware: Processing Bearer token.");
            string token = authHeader["Bearer ".Length..].Trim();
            keys.Context.LEXISToken = token;
            
            try
            {
                await keys.Authorize(sshCaService, userOrgService);
                var identity = new ClaimsIdentity(new[] { new Claim("raw_token", token) }, "Lexis");
                context.User = new ClaimsPrincipal(identity);
                Log.Info("AuthMiddleware: Internal Authorize success.");

                // Add to LogContext early so Auth logs also get the user
                HEAppE.Utils.LoggingUtils.AddUserPropertiesToLogThreadContext(
                    keys.Context.AdaptorUserId, keys.Context.UserName, keys.Context.Email);
            }
            catch (Exception ex)
            {
                Log.Error($"AuthMiddleware: Internal Authorize failed: {ex.Message}");
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }
        else
        {
            Log.Info("AuthMiddleware: Falling back to LocalScheme.");
            var identity = new ClaimsIdentity(new[] { new Claim("raw_token", string.Empty) }, "LocalScheme");
            context.User = new ClaimsPrincipal(identity);
        }

        await _next(context);
    }
}
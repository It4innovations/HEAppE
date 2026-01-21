using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Authorization;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier;

public class LexisAuthMiddleware
{
    private readonly RequestDelegate _next;

    public LexisAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IHttpContextKeys keys, ISshCertificateAuthorityService sshCaService, IUserOrgService userOrgService)
    {
        // check if the endpoint allows anonymous access
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            await _next(context);
            return;
        }

        string authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (LexisAuthenticationConfiguration.UseBearerAuth && authHeader?.StartsWith("Bearer ") == true)
        {
            string token = authHeader["Bearer ".Length..].Trim();
            keys.Context.LEXISToken = token;
            
            try
            {
                await keys.Authorize(sshCaService, userOrgService);
                var identity = new ClaimsIdentity(new[] { new Claim("raw_token", token) }, "Lexis");
                context.User = new ClaimsPrincipal(identity);
            }
            catch
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }
        else
        {
            //context.Response.StatusCode = 401;
            //await context.Response.WriteAsync("Unauthorized");
            //no token provided, proceed as local user
            var identity = new ClaimsIdentity(new[] { new Claim("raw_token", string.Empty) }, "LocalScheme");
            context.User = new ClaimsPrincipal(identity);
            await _next(context);
            return;
        }

        await _next(context);
    }
}
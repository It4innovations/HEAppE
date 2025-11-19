using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier;

public class LexisAuthMiddleware
{
    private readonly RequestDelegate _next;

    public LexisAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IHttpContextKeys keys, ISshCertificateAuthorityService sshCaService)
    {
        string authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") == true)
        {
            string token = authHeader["Bearer ".Length..].Trim();
            keys.Context.LEXISToken = token;

            // zde můžeš provést vlastní authorize
            try
            {
                await keys.Authorize(sshCaService); // předpokládá, že můžeš předat token
                // případně vytvořit ClaimsPrincipal pro HttpContext.User
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
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }
}

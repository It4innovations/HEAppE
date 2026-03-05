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
        // Log Request
        context.Request.EnableBuffering();
        string requestBody = string.Empty;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        Log.Info($"[Request] Method: {context.Request.Method}, Path: {context.Request.Path}, Query: {context.Request.QueryString}, Body: {requestBody}");

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null)
        {
            Log.Info("AuthMiddleware: Anonymous endpoint detected.");
            await ProceedWithResponseLogging(context);
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

        await ProceedWithResponseLogging(context);
    }

    private async Task ProceedWithResponseLogging(HttpContext context)
    {
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            if (responseBody.CanRead)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                string responseText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);

                Log.Debug($"[Response] Path: {context.Request.Path}, StatusCode: {context.Response.StatusCode}, Body: {responseText}");
                
                await responseBody.CopyToAsync(originalBodyStream);
            }
            context.Response.Body = originalBodyStream;
        }
    }
}
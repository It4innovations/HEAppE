using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Services.UserOrg;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using log4net;
using SshCaAPI;
using SshCaAPI.Configuration;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

public static class JwtIntrospectionExtensions
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(JwtIntrospectionExtensions));

    public static IServiceCollection AddSmartAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        if (true)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "SmartScheme";
                options.DefaultAuthenticateScheme = "SmartScheme";
                options.DefaultChallengeScheme = "SmartScheme";
            })
            .AddPolicyScheme("SmartScheme", "Local or JWT", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (JwtTokenIntrospectionConfiguration.IsEnabled && !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Debug($"[SmartScheme] Path: {context.Request.Path} -> OAuth2Introspection");
                        return OAuth2IntrospectionDefaults.AuthenticationScheme;
                    }
                    Log.Debug($"[SmartScheme] Path: {context.Request.Path} -> LocalScheme");
                    return "LocalScheme";
                };
            })
            .AddScheme<AuthenticationSchemeOptions, LocalAuthenticationHandler>("LocalScheme", null);

            services.AddAuthentication()
                .AddOAuth2Introspection(OAuth2IntrospectionDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = JwtTokenIntrospectionConfiguration.Authority;
                    options.ClientId = JwtTokenIntrospectionConfiguration.ClientId;
                    options.ClientSecret = JwtTokenIntrospectionConfiguration.ClientSecret;
                    options.EnableCaching = true;
                    options.CacheDuration = TimeSpan.FromMinutes(5);
                    options.DiscoveryPolicy = new DiscoveryPolicy
                    {
                        ValidateIssuerName = JwtTokenIntrospectionConfiguration.ValidateIssuerName,
                        RequireHttps = JwtTokenIntrospectionConfiguration.RequireHttps,
                        ValidateEndpoints = JwtTokenIntrospectionConfiguration.ValidateEndpoints
                    };

                    options.TokenRetriever = request =>
                    {
                        var authHeader = request.Headers["Authorization"].FirstOrDefault();
                        return authHeader?.StartsWith("Bearer ") == true ? authHeader["Bearer ".Length..].Trim() : null;
                    };

                    options.Events = new OAuth2IntrospectionEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            Log.Error($"[Introspection] Auth Failed. Error: {context.Error}. Path: {context.HttpContext.Request.Path}");
                            context.Fail("Invalid token or not active");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async context =>
                        {
                            Log.Info($"[Introspection] Token Validated. User: {context.Principal?.Identity?.Name}. Claims: {string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}"))}");

                            var sshCaService = context.HttpContext.RequestServices.GetRequiredService<ISshCertificateAuthorityService>();
                            var userOrgService = context.HttpContext.RequestServices.GetRequiredService<IUserOrgService>();

                            try
                            {
                                await context.HttpContext.RequestServices.GetRequiredService<IHttpContextKeys>().Authorize(sshCaService, userOrgService);
                                Log.Debug("[Introspection] Internal Authorization Success");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"[Introspection] Internal Authorization Failed: {ex.Message}");
                                context.Fail("Unauthorized");
                                return;
                            }

                            if (JwtTokenIntrospectionConfiguration.IsEnabled && SshCaSettings.UseCertificateAuthorityForAuthentication)
                            {
                                var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                                var client = httpClientFactory.CreateClient();
                         
                                string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
                                string version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
                                //add user agent
                                client.DefaultRequestHeaders.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
                                //get token endpoint from discovery document
                                var disco = await client.GetDiscoveryDocumentAsync(JwtTokenIntrospectionConfiguration.Authority);
                                if (disco.IsError)                                {
                                    Log.Error($"[Introspection] Discovery document retrieval failed: {disco.Error}");
                                    context.Fail("Token exchange failed");
                                    return;
                                }
                                else
                                {
                                    Log.Debug($"[Introspection] Discovery document retrieved successfully. Token endpoint: {disco.TokenEndpoint}");
                                }
                                await context.HttpContext.RequestServices.GetRequiredService<IHttpContextKeys>().ExchangeSshCaToken(disco.TokenEndpoint, client);
                            }
                        }
                    };
                });

            services.AddTransient<LoggingHandler>();
            services.AddHttpClient(OAuth2IntrospectionDefaults.BackChannelHttpClientName)
                .AddHttpMessageHandler<LoggingHandler>()
                .ConfigureHttpClient(client =>
                {
                    var version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
                    var instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
                    client.DefaultRequestHeaders.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
                });
        }
        return services;
    }
}

public class LoggingHandler : DelegatingHandler
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(LoggingHandler));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : "[empty]";
        Log.Debug($"[Introspection Request] {request.Method} {request.RequestUri} Content: {requestContent}");

        var response = await base.SendAsync(request, cancellationToken);

        var responseContent = response.Content != null ? await response.Content.ReadAsStringAsync(cancellationToken) : "[empty]";
        Log.Debug($"[Introspection Response] Status: {response.StatusCode} Content: {responseContent}");

        return response;
    }
}
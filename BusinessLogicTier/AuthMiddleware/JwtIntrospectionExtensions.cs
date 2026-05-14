using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.Services.UserOrg;
using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using log4net;
using SshCaAPI;
using SshCaAPI.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

public static class JwtIntrospectionExtensions
{
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
                    var log = context.RequestServices.GetRequiredService<ILoggerFactory>()
                        .CreateLogger(nameof(JwtIntrospectionExtensions));
                    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                    if (JwtTokenIntrospectionConfiguration.IsEnabled && !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        log.LogDebug("[SmartScheme] Path: {Path} -> OAuth2Introspection", context.Request.Path);
                        return OAuth2IntrospectionDefaults.AuthenticationScheme;
                    }
                    log.LogDebug("[SmartScheme] Path: {Path} -> LocalScheme", context.Request.Path);
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
                            var log = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                                .CreateLogger(nameof(JwtIntrospectionExtensions));
                            log.LogError("[Introspection] Auth Failed (Keycloak). Error: {Error}. Path: {Path}", context.Error, context.HttpContext.Request.Path);
                            context.Fail($"Authentication failed (Keycloak): {context.Error}");
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async context =>
                        {
                            var log = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                                .CreateLogger(nameof(JwtIntrospectionExtensions));
                            log.LogInformation("[Introspection] Token Validated. User: {User}. Claims: {Claims}",
                                context.Principal?.Identity?.Name,
                                string.Join(", ", context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}")));

                            var sshCaService = context.HttpContext.RequestServices.GetRequiredService<ISshCertificateAuthorityService>();
                            var userOrgService = context.HttpContext.RequestServices.GetRequiredService<IUserOrgService>();
                            var expirioService = context.HttpContext.RequestServices.GetRequiredService<IExpirioService>();

                            try
                            {
                                await context.HttpContext.RequestServices.GetRequiredService<IHttpContextKeys>().Authorize(sshCaService, userOrgService, expirioService);
                                log.LogDebug("[Introspection] Internal Authorization Success");
                            }
                            catch (Exception ex)
                            {
                                string serviceInfo = ex is HEAppE.Exceptions.AbstractTypes.ExternalException ee && !string.IsNullOrEmpty(ee.ServiceName) ? $" ({ee.ServiceName})" : "";
                                log.LogError("[Introspection] Internal Authorization Failed{ServiceInfo}: {Message}", serviceInfo, ex.Message);
                                context.Fail($"Internal authorization failed{serviceInfo}: {ex.Message}");
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
                                    log.LogError("[Introspection] Discovery document retrieval failed (Keycloak): {Error}", disco.Error);
                                    context.Fail($"Token exchange failed (Keycloak discovery error): {disco.Error}");
                                    return;
                                }
                                else
                                {
                                    log.LogDebug("[Introspection] Discovery document retrieved successfully. Token endpoint: {TokenEndpoint}", disco.TokenEndpoint);
                                }
                                try
                                {
                                    await context.HttpContext.RequestServices.GetRequiredService<IHttpContextKeys>().ExchangeSshCaToken(disco.TokenEndpoint, client);
                                }
                                catch (Exception ex)
                                {
                                    log.LogError(ex, "[Introspection] SSH CA token exchange failed: {Message}", ex.Message);
                                    context.Fail($"SSH CA token exchange failed: {ex.Message}");
                                    return;
                                }
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
    private readonly ILogger<LoggingHandler> _log;

    public LoggingHandler(ILogger<LoggingHandler> logger)
    {
        _log = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
    {
        var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync(cancellationToken) : "[empty]";
        _log.LogDebug("[Introspection Request] {Method} {RequestUri} Content: {Content}", request.Method, request.RequestUri, requestContent);

        var response = await base.SendAsync(request, cancellationToken);

        var responseContent = response.Content != null ? await response.Content.ReadAsStringAsync(cancellationToken) : "[empty]";
        _log.LogDebug("[Introspection Response] Status: {StatusCode} Content: {Content}", response.StatusCode, responseContent);

        return response;
    }
}
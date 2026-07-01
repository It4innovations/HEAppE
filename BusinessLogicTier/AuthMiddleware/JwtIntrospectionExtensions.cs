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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HEAppE.Authentication;

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

                                var problem = new ProblemDetails
                                {
                                    Status = StatusCodes.Status401Unauthorized,
                                    Title = "Unauthorized Access",
                                    Detail = ex.Message
                                };

                                if (ex is HEAppE.Exceptions.External.AuthenticationTypeException authEx)
                                {
                                    problem.Title = authEx.ServiceName != null ? $"Unauthorized Access ({authEx.ServiceName})" : "Unauthorized Access";
                                    problem.Detail = authEx.Message + (authEx.Details != null ? $": {authEx.Details}" : "");
                                }
                                else if (ex is ExternalException externalEx)
                                {
                                    problem.Status = StatusCodes.Status502BadGateway;
                                    problem.Title = !string.IsNullOrEmpty(externalEx.ServiceName) ? $"External Problem ({externalEx.ServiceName})" : "External Problem";
                                    problem.Detail = externalEx.Message + (externalEx.Details != null ? $": {externalEx.Details}" : "");
                                }

                                var response = context.HttpContext.Response;
                                response.ContentType = "application/json";
                                response.StatusCode = problem.Status.Value;
                                await response.WriteAsJsonAsync(problem);

                                context.Fail(ex);
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

                                    var problem = new ProblemDetails
                                    {
                                        Status = StatusCodes.Status502BadGateway,
                                        Title = "External Problem (Keycloak)",
                                        Detail = $"Token exchange failed: Keycloak discovery document retrieval error: {disco.Error}"
                                    };

                                    var response = context.HttpContext.Response;
                                    response.ContentType = "application/json";
                                    response.StatusCode = problem.Status.Value;
                                    await response.WriteAsJsonAsync(problem);

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

                                    var problem = new ProblemDetails
                                    {
                                        Status = StatusCodes.Status401Unauthorized,
                                        Title = "SSH CA Token Exchange Failed",
                                        Detail = ex.Message
                                    };

                                    if (ex is ExternalException externalEx)
                                    {
                                        problem.Status = StatusCodes.Status502BadGateway;
                                        problem.Title = !string.IsNullOrEmpty(externalEx.ServiceName) ? $"SSH CA Token Exchange External Problem ({externalEx.ServiceName})" : "SSH CA Token Exchange External Problem";
                                        problem.Detail = externalEx.Message + (externalEx.Details != null ? $": {externalEx.Details}" : "");
                                    }

                                    var response = context.HttpContext.Response;
                                    response.ContentType = "application/json";
                                    response.StatusCode = problem.Status.Value;
                                    await response.WriteAsJsonAsync(problem);

                                    context.Fail(ex);
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

            services.AddHttpClient<IJwtTokenIntrospectionService, HEAppE.Authentication.JwtTokenIntrospectionService>(client =>
            {
                if (!string.IsNullOrEmpty(JwtTokenIntrospectionConfiguration.Authority))
                {
                    client.BaseAddress = new Uri(JwtTokenIntrospectionConfiguration.Authority);
                }
                var version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
                var instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
                client.DefaultRequestHeaders.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(HEAppE.RestUtils.ResiliencePolicies.TransientRetryPolicy)
            .AddPolicyHandler(HEAppE.RestUtils.ResiliencePolicies.DefaultCircuitBreakerPolicy);
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
        _log.LogDebug("[Introspection Request] {Method} {RequestUri}", request.Method, request.RequestUri);

        var response = await base.SendAsync(request, cancellationToken);

        _log.LogDebug("[Introspection Response] Status: {StatusCode}", response.StatusCode);

        return response;
    }
}
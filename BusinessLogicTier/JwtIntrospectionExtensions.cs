using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.HpcConnectionFramework.Configuration;
using log4net;
using SshCaAPI;

public static class JwtIntrospectionExtensions
{
    public static IServiceCollection AddSmartAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register OAuth2 Introspection if enabled
        if (true)
        {
            // Default authentication scheme with runtime selection
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
        
                    // If Bearer token is present, use OAuth2 Introspection
                    if ((JwtTokenIntrospectionConfiguration.IsEnabled) && !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        return OAuth2IntrospectionDefaults.AuthenticationScheme;
                    }

                    // Otherwise (no header or custom API Key header), route to LocalScheme or LexisAuthenticationConfiguration.UseBearerAuth
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
                        if (authHeader?.StartsWith("Bearer ") != true)
                            return null; // skip introspection → local login
                        return authHeader["Bearer ".Length..].Trim();
                    };

                    options.Events = new OAuth2IntrospectionEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            if (string.IsNullOrEmpty(context.SecurityToken))
                                return; // local login, skip introspection

                            var sshCaService = context.HttpContext.RequestServices
                                .GetRequiredService<ISshCertificateAuthorityService>();
                            var userOrgService = context.HttpContext.RequestServices
                                .GetRequiredService<IUserOrgService>();

                            try
                            {
                                await context.HttpContext.RequestServices
                                    .GetRequiredService<IHttpContextKeys>()
                                    .Authorize(sshCaService, userOrgService);
                            }
                            catch
                            {
                                context.Fail("Unauthorized");
                                return;
                            }

                            // Optional: exchange SSH CA token
                            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                            var client = httpClientFactory.CreateClient();
                      
                            string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
                            string version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
                            client.DefaultRequestHeaders.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");

                            var disco = await client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
                            {
                                Address = JwtTokenIntrospectionConfiguration.Authority,
                                Policy = new DiscoveryPolicy
                                {
                                    RequireHttps = JwtTokenIntrospectionConfiguration.RequireHttps,
                                    ValidateIssuerName = JwtTokenIntrospectionConfiguration.ValidateIssuerName,
                                    ValidateEndpoints = JwtTokenIntrospectionConfiguration.ValidateEndpoints
                                }
                            });

                            if (disco.IsError)
                                throw new Exception($"Discovery error: {disco.Error}");

                            await context.HttpContext.RequestServices
                                .GetRequiredService<IHttpContextKeys>()
                                .ExchangeSshCaToken(disco.TokenEndpoint, client);
                        }
                    };
                });

            // Add user-agent for introspection backchannel
            services.AddHttpClient(OAuth2IntrospectionDefaults.BackChannelHttpClientName)
                .ConfigureHttpClient(client =>
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("HEAppE Middleware Dev/1.0");
                });
        }

        return services;
    }
}

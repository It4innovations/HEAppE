using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier;
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SshCaAPI;


public static class JwtIntrospectionExtensions
{
    public static IServiceCollection AddJwtIntrospectionIfEnabled(this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!JwtTokenIntrospectionConfiguration.IsEnabled)
            return services;

        // Register OAuth2 Introspection
        services.AddAuthentication(OAuth2IntrospectionDefaults.AuthenticationScheme)
            .AddOAuth2Introspection(options =>
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
                    string authHeader = request.Headers["Authorization"].FirstOrDefault();
                    if (authHeader?.StartsWith("Bearer ") != true)
                        return null;
                    var incomingToken = authHeader["Bearer ".Length..].Trim();
                    return incomingToken;
                };


                options.Events = new OAuth2IntrospectionEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sshCaService = context.HttpContext.RequestServices
                            .GetRequiredService<ISshCertificateAuthorityService>();
                        
                        if(string.IsNullOrEmpty(context.SecurityToken))
                        {
                            //local user, no token to validate
                            return;
                        }
                        
                        try
                        {
                            await context.HttpContext.RequestServices
                                .GetRequiredService<IHttpContextKeys>()
                                .Authorize(sshCaService);
                        }
                        catch (Exception ex)
                        {
                            context.Fail("Unauthorized");
                            return;
                        }

                        var httpClientFactory =
                            context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
                        var client = httpClientFactory.CreateClient();
                        client.DefaultRequestHeaders.UserAgent.ParseAdd("HEAppE Middleware Dev/1.0");

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

                        var sshCaToken = await context.HttpContext.RequestServices
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

        return services;
    }
}
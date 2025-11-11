using IdentityModel.AspNetCore.OAuth2Introspection;
using IdentityModel.Client;
using System;
using System.Linq;
using System.Net.Http;
using HEAppE.BusinessLogicTier;
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;


public static class JwtIntrospectionExtensions
{
    public static IServiceCollection AddJwtIntrospectionIfEnabled(this IServiceCollection services, IConfiguration configuration)
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
                    var authHeader = request.Headers["Authorization"].FirstOrDefault();
                    return authHeader?.StartsWith("Bearer ") == true
                        ? authHeader["Bearer ".Length..].Trim()
                        : null;
                };

                options.Events = new OAuth2IntrospectionEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sshCaService = context.HttpContext.RequestServices.GetRequiredService<ISshCertificateAuthorityService>();
                        try
                        {
                            //await HttpContextKeys.Authorize(context.SecurityToken, sshCaService);
                            await services.BuildServiceProvider()
                                .GetRequiredService<IHttpContextKeys>()
                                .Authorize(context.SecurityToken, sshCaService);
                        }
                        catch
                        {
                            context.Fail("Unauthorized");
                            return;
                        }

                        var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();
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

                        

                        //await HttpContextKeys.ExchangeSshCaToken(context.SecurityToken, disco.TokenEndpoint, client);
                        var sshCaToken = await services.BuildServiceProvider()
                            .GetRequiredService<IHttpContextKeys>()
                            .ExchangeSshCaToken(context.SecurityToken, disco.TokenEndpoint, client);
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
using System;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.DependencyInjection;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

/// <summary>
/// Resolves the appropriate <see cref="IUserRoleSourceProvider"/> based on configuration.
/// </summary>
public static class UserRoleSourceProviderResolver
{
    public static IUserRoleSourceProvider Resolve(
        IServiceProvider? serviceProvider,
        IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCaService,
        IHttpContextKeys httpContextKeys,
        IExpirioService expirioService)
    {
        var configuredProvider = UserRoleSourceConfiguration.Provider;

        // Auto-detection: if UserOrg is configured but BaseAddress is empty and OIDC is enabled,
        // automatically fallback to OpenIdClaims provider so requests do not fail on unconfigured UserOrg.
        if (configuredProvider == UserRoleSourceType.UserOrg &&
            string.IsNullOrWhiteSpace(LexisAuthenticationConfiguration.BaseAddress) &&
            JwtTokenIntrospectionConfiguration.IsEnabled)
        {
            return new OpenIdClaimsUserRoleSourceProvider();
        }

        return configuredProvider switch
        {
            UserRoleSourceType.OpenIdClaims => new OpenIdClaimsUserRoleSourceProvider(),
            UserRoleSourceType.Database => new DatabaseUserRoleSourceProvider(),
            _ => new UserOrgUserRoleSourceProvider(userOrgService, sshCaService, httpContextKeys, expirioService)
        };
    }
}

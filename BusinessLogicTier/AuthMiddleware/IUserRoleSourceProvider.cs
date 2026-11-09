using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

/// <summary>
/// Strategy provider responsible for resolving user identity and synchronizing project memberships/roles.
/// </summary>
public interface IUserRoleSourceProvider
{
    /// <summary>
    /// Type of this provider (UserOrg, OpenIdClaims, Database).
    /// </summary>
    UserRoleSourceType ProviderType { get; }

    /// <summary>
    /// Resolves the AdaptorUser, auto-creates if configured, and performs role/project synchronization.
    /// </summary>
    /// <param name="claimsPrincipal">Validated OIDC ClaimsPrincipal (if available).</param>
    /// <param name="rawToken">Raw Bearer token string.</param>
    /// <param name="unitOfWork">Current UnitOfWork.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>Authenticated and authorized AdaptorUser.</returns>
    Task<AdaptorUser> ResolveAndSyncUserAsync(
        ClaimsPrincipal? claimsPrincipal,
        string? rawToken,
        IUnitOfWork unitOfWork,
        ILogger logger);
}

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

/// <summary>
/// User and role provider where identity is validated via OIDC, but projects and roles
/// are managed exclusively by HEAppE administrators in the HEAppE database.
/// </summary>
public class DatabaseUserRoleSourceProvider : IUserRoleSourceProvider
{
    public UserRoleSourceType ProviderType => UserRoleSourceType.Database;

    public async Task<AdaptorUser> ResolveAndSyncUserAsync(
        ClaimsPrincipal? claimsPrincipal,
        string? rawToken,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        logger.LogInformation("[DatabaseUserRoleSourceProvider] Resolving user identity for local DB role management.");

        string? username = claimsPrincipal?.FindFirst(ClaimTypes.Name)?.Value
                           ?? claimsPrincipal?.FindFirst("preferred_username")?.Value
                           ?? claimsPrincipal?.FindFirst("username")?.Value;
        string? email = claimsPrincipal?.FindFirst(ClaimTypes.Email)?.Value
                        ?? claimsPrincipal?.FindFirst("email")?.Value;
        string? idpSid = claimsPrincipal?.FindFirst("sub")?.Value
                         ?? claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(username))
        {
            if (!string.IsNullOrWhiteSpace(email))
                username = email;
            else if (!string.IsNullOrWhiteSpace(idpSid))
                username = idpSid;
            else
                throw new AuthenticationTypeException("InvalidToken");
        }

        AdaptorUser? user = null;
        if (!string.IsNullOrEmpty(idpSid) && idpSid != Guid.Empty.ToString())
        {
            user = await unitOfWork.AdaptorUserRepository.GetByIdpSidIgnoreQueryFiltersAsync(idpSid);
        }

        if (user == null && !string.IsNullOrEmpty(username))
        {
            user = await unitOfWork.AdaptorUserRepository.GetByNameIgnoreQueryFiltersAsync(username);
        }

        if (user == null && !string.IsNullOrEmpty(email))
        {
            user = await unitOfWork.AdaptorUserRepository.GetByEmailIgnoreQueryFiltersAsync(email);
        }

        if (user == null)
        {
            if (!DatabaseSourceConfiguration.AutoCreateUser)
            {
                logger.LogWarning($"[DatabaseUserRoleSourceProvider] User '{username}' does not exist in HEAppE and AutoCreateUser is false.");
                throw new AuthenticationTypeException("UserNotRegisteredInHEAppE", username);
            }

            logger.LogInformation($"[DatabaseUserRoleSourceProvider] Creating new AdaptorUser '{username}'.");
            user = new AdaptorUser
            {
                Username = username,
                Email = email ?? string.Empty,
                IdpSid = idpSid,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = null,
                UserType = AdaptorUserType.OpenId,
                Synchronize = false,
                IsDeleted = false
            };

            unitOfWork.AdaptorUserRepository.Insert(user);
            await unitOfWork.SaveAsync();
        }

        if (user.IsBlocked)
        {
            logger.LogWarning($"[DatabaseUserRoleSourceProvider] User '{user.Username}' is blocked.");
            throw new UnauthorizedAccessException($"User '{user.Username}' is blocked.");
        }

        // Check if user has active roles if AllowUnassignedUsers is false
        if (!DatabaseSourceConfiguration.AllowUnassignedUsers)
        {
            bool hasRoles = user.AdaptorUserUserGroupRoles.Any(r => !r.IsDeleted);
            if (!hasRoles)
            {
                logger.LogWarning($"[DatabaseUserRoleSourceProvider] User '{user.Username}' has no assigned project roles in HEAppE.");
                throw new AuthenticationTypeException("NoUserGroup", user.Username);
            }
        }

        return user;
    }
}

using System.Security.Claims;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

/// <summary>
/// User and role provider delegating to the external UserOrg API (Lexis platform).
/// </summary>
public class UserOrgUserRoleSourceProvider : IUserRoleSourceProvider
{
    private readonly IUserOrgService _userOrgService;
    private readonly ISshCertificateAuthorityService _sshCertificateAuthorityService;
    private readonly IHttpContextKeys _httpContextKeys;
    private readonly IExpirioService _expirioService;

    public UserOrgUserRoleSourceProvider(
        IUserOrgService userOrgService,
        ISshCertificateAuthorityService sshCertificateAuthorityService,
        IHttpContextKeys httpContextKeys,
        IExpirioService expirioService)
    {
        _userOrgService = userOrgService;
        _sshCertificateAuthorityService = sshCertificateAuthorityService;
        _httpContextKeys = httpContextKeys;
        _expirioService = expirioService;
    }

    public UserRoleSourceType ProviderType => UserRoleSourceType.UserOrg;

    public async Task<AdaptorUser> ResolveAndSyncUserAsync(
        ClaimsPrincipal? claimsPrincipal,
        string? rawToken,
        IUnitOfWork unitOfWork,
        ILogger logger)
    {
        logger.LogInformation("[UserOrgUserRoleSourceProvider] Starting UserOrg authorization flow.");

        var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(
            unitOfWork, _userOrgService, _sshCertificateAuthorityService, _httpContextKeys, _expirioService, logger);

        return await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
        {
            OpenIdLexisAccessToken = rawToken
        });
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.Logging;
using Moq;
using SshCaAPI;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class OpenIdClaimsUserRoleSourceProviderTests : IDisposable
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IAdaptorUserRepository> _mockUserRepo = new();
    private readonly Mock<IProjectRepository> _mockProjectRepo = new();
    private readonly Mock<IAdaptorUserGroupRepository> _mockGroupRepo = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public OpenIdClaimsUserRoleSourceProviderTests()
    {
        ResetConfiguration();
        _mockUnitOfWork.Setup(u => u.AdaptorUserRepository).Returns(_mockUserRepo.Object);
        _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepo.Object);
        _mockUnitOfWork.Setup(u => u.AdaptorUserGroupRepository).Returns(_mockGroupRepo.Object);
    }

    public void Dispose()
    {
        ResetConfiguration();
    }

    private static void ResetConfiguration()
    {
        UserRoleSourceConfiguration.Provider = UserRoleSourceType.UserOrg;
        OpenIdClaimsConfiguration.AutoCreateUser = true;
        OpenIdClaimsConfiguration.UsernameClaim = "preferred_username";
        OpenIdClaimsConfiguration.EmailClaim = "email";
        OpenIdClaimsConfiguration.GroupClaimName = "groups";
        OpenIdClaimsConfiguration.GroupRegexPattern = null;
        OpenIdClaimsConfiguration.NotationPattern = null;
        OpenIdClaimsConfiguration.DefaultRoles = new List<string> { "Submitter", "Reporter" };
        OpenIdClaimsConfiguration.AssignToAllActiveProjects = false;
        OpenIdClaimsConfiguration.RoleMapping = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        DatabaseSourceConfiguration.AutoCreateUser = true;
        DatabaseSourceConfiguration.AllowUnassignedUsers = false;

        JwtTokenIntrospectionConfiguration.IsEnabled = false;
        LexisAuthenticationConfiguration.BaseAddress = null;
    }

    #region Provider Resolver Tests

    [Fact]
    public void Resolver_WhenConfiguredOpenIdClaims_ReturnsOpenIdClaimsProvider()
    {
        UserRoleSourceConfiguration.Provider = UserRoleSourceType.OpenIdClaims;

        var provider = UserRoleSourceProviderResolver.Resolve(
            null,
            Mock.Of<IUserOrgService>(),
            Mock.Of<ISshCertificateAuthorityService>(),
            Mock.Of<IHttpContextKeys>(),
            Mock.Of<IExpirioService>());

        provider.Should().BeOfType<OpenIdClaimsUserRoleSourceProvider>();
        provider.ProviderType.Should().Be(UserRoleSourceType.OpenIdClaims);
    }

    [Fact]
    public void Resolver_WhenConfiguredDatabase_ReturnsDatabaseProvider()
    {
        UserRoleSourceConfiguration.Provider = UserRoleSourceType.Database;

        var provider = UserRoleSourceProviderResolver.Resolve(
            null,
            Mock.Of<IUserOrgService>(),
            Mock.Of<ISshCertificateAuthorityService>(),
            Mock.Of<IHttpContextKeys>(),
            Mock.Of<IExpirioService>());

        provider.Should().BeOfType<DatabaseUserRoleSourceProvider>();
        provider.ProviderType.Should().Be(UserRoleSourceType.Database);
    }

    [Fact]
    public void Resolver_WhenConfiguredUserOrgWithBaseAddress_ReturnsUserOrgProvider()
    {
        UserRoleSourceConfiguration.Provider = UserRoleSourceType.UserOrg;
        LexisAuthenticationConfiguration.BaseAddress = "https://userorg.example.com";

        var provider = UserRoleSourceProviderResolver.Resolve(
            null,
            Mock.Of<IUserOrgService>(),
            Mock.Of<ISshCertificateAuthorityService>(),
            Mock.Of<IHttpContextKeys>(),
            Mock.Of<IExpirioService>());

        provider.Should().BeOfType<UserOrgUserRoleSourceProvider>();
        provider.ProviderType.Should().Be(UserRoleSourceType.UserOrg);
    }

    [Fact]
    public void Resolver_WhenUserOrgConfiguredButBaseAddressEmptyAndIntrospectionEnabled_AutoFallbacksToOpenIdClaims()
    {
        UserRoleSourceConfiguration.Provider = UserRoleSourceType.UserOrg;
        LexisAuthenticationConfiguration.BaseAddress = ""; // Empty UserOrg
        JwtTokenIntrospectionConfiguration.IsEnabled = true; // Introspection enabled

        var provider = UserRoleSourceProviderResolver.Resolve(
            null,
            Mock.Of<IUserOrgService>(),
            Mock.Of<ISshCertificateAuthorityService>(),
            Mock.Of<IHttpContextKeys>(),
            Mock.Of<IExpirioService>());

        provider.Should().BeOfType<OpenIdClaimsUserRoleSourceProvider>();
    }

    #endregion

    #region OpenIdClaims Claims Resolution & User Registration Tests

    [Fact]
    public async Task ResolveAndSyncUserAsync_WithPreferredUsername_CreatesAndReturnsUser()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "di35dal"),
            new Claim("email", "dzung.nguyen@lrz.de"),
            new Claim("sub", "keycloak-user-guid-1234"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        SetupMockProjectAndGroup("pr74qo", 101, 201);

        _mockUserRepo.Setup(r => r.GetByIdpSidIgnoreQueryFiltersAsync("keycloak-user-guid-1234"))
            .ReturnsAsync((AdaptorUser?)null);
        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("di35dal"))
            .ReturnsAsync((AdaptorUser?)null);
        _mockUserRepo.Setup(r => r.GetByEmailIgnoreQueryFiltersAsync("dzung.nguyen@lrz.de"))
            .ReturnsAsync((AdaptorUser?)null);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.Should().NotBeNull();
        user.Username.Should().Be("di35dal");
        user.Email.Should().Be("dzung.nguyen@lrz.de");
        user.IdpSid.Should().Be("keycloak-user-guid-1234");
        user.UserType.Should().Be(AdaptorUserType.OpenId);

        _mockUserRepo.Verify(r => r.Insert(It.Is<AdaptorUser>(u => u.Username == "di35dal")), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenUsernameMissing_FallsBackToEmail()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("email", "testuser@lrz.de"),
            new Claim("sub", "sub-guid-123"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        SetupMockProjectAndGroup("pr74qo", 101, 201);

        _mockUserRepo.Setup(r => r.GetByIdpSidIgnoreQueryFiltersAsync("sub-guid-123"))
            .ReturnsAsync((AdaptorUser?)null);
        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser@lrz.de"))
            .ReturnsAsync((AdaptorUser?)null);
        _mockUserRepo.Setup(r => r.GetByEmailIgnoreQueryFiltersAsync("testuser@lrz.de"))
            .ReturnsAsync((AdaptorUser?)null);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.Username.Should().Be("testuser@lrz.de");
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenUsernameAndEmailMissing_FallsBackToSub()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "sub-guid-456"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        SetupMockProjectAndGroup("pr74qo", 101, 201);

        _mockUserRepo.Setup(r => r.GetByIdpSidIgnoreQueryFiltersAsync("sub-guid-456"))
            .ReturnsAsync((AdaptorUser?)null);
        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("sub-guid-456"))
            .ReturnsAsync((AdaptorUser?)null);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.Username.Should().Be("sub-guid-456");
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenNoIdentityClaims_ThrowsAuthenticationTypeException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var act = () => provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        await act.Should().ThrowAsync<AuthenticationTypeException>();
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenUserNotFoundAndAutoCreateFalse_ThrowsAuthenticationTypeException()
    {
        OpenIdClaimsConfiguration.AutoCreateUser = false;

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "unknown_user"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("unknown_user"))
            .ReturnsAsync((AdaptorUser?)null);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var act = () => provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        await act.Should().ThrowAsync<AuthenticationTypeException>();
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenUserBlocked_ThrowsUnauthorizedAccessException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "blocked_user"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        var blockedUser = new AdaptorUser
        {
            Id = 1,
            Username = "blocked_user",
            IsBlocked = true
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("blocked_user"))
            .ReturnsAsync(blockedUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var act = () => provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*blocked*");
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenExistingUserHasUpdatedEmailOrSub_UpdatesUserInDatabase()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "existing_user"),
            new Claim("email", "new_email@example.com"),
            new Claim("sub", "new-sub-guid"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        SetupMockProjectAndGroup("pr74qo", 101, 201);

        var existingUser = new AdaptorUser
        {
            Id = 42,
            Username = "existing_user",
            Email = "old_email@example.com",
            IdpSid = "old-sub-guid",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByIdpSidIgnoreQueryFiltersAsync("new-sub-guid"))
            .ReturnsAsync((AdaptorUser?)null);
        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("existing_user"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.Email.Should().Be("new_email@example.com");
        user.IdpSid.Should().Be("new-sub-guid");
        _mockUserRepo.Verify(r => r.Update(existingUser), Times.Once);
    }

    #endregion

    #region OpenIdClaims Role & Project Synchronization Tests

    [Theory]
    [InlineData("/hpda/heappe-pr74qo-Submitter", "pr74qo", AdaptorUserRoleType.Submitter)]
    [InlineData("/heappe/pr74qo-Manager", "pr74qo", AdaptorUserRoleType.Manager)]
    [InlineData("/heappe-pr74qo-Reporter", "pr74qo", AdaptorUserRoleType.Reporter)]
    [InlineData("/projects/TERRABYTE-00/Submitter", "TERRABYTE-00", AdaptorUserRoleType.Submitter)]
    [InlineData("/dss/TERRABYTE-01/Manager", "TERRABYTE-01", AdaptorUserRoleType.Manager)]
    public async Task ResolveAndSyncUserAsync_TerrabyteGroupConventions_AssignsExpectedProjectAndRole(
        string groupClaim, string expectedProject, AdaptorUserRoleType expectedRole)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser"),
            new Claim("groups", groupClaim)
        }));

        SetupMockProjectAndGroup(expectedProject, 10, 20);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.AdaptorUserUserGroupRoles.Should().ContainSingle(r =>
            r.AdaptorUserGroupId == 20 &&
            r.AdaptorUserRoleId == (long)expectedRole &&
            !r.IsDeleted);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_MultipleGroups_AssignsMultipleRoles()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "di35dal"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter"),
            new Claim("groups", "/hpda/heappe-pr74qo-Manager")
        }));

        SetupMockProjectAndGroup("pr74qo", 101, 201);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "di35dal",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("di35dal"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.AdaptorUserUserGroupRoles.Should().HaveCount(2);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Submitter && !r.IsDeleted);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Manager && !r.IsDeleted);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_CustomNotationPattern_MapsProjectAndRoleCorrectly()
    {
        OpenIdClaimsConfiguration.NotationPattern = "/{root}/heappe-{project}-{role}";

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser"),
            new Claim("groups", "/customroot/heappe-myproject-Manager")
        }));

        SetupMockProjectAndGroup("myproject", 10, 20);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.AdaptorUserUserGroupRoles.Should().ContainSingle(r =>
            r.AdaptorUserGroupId == 20 &&
            r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Manager &&
            !r.IsDeleted);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_RoleMappingAliases_MapsConfiguredAliases()
    {
        OpenIdClaimsConfiguration.RoleMapping["member"] = new List<string> { "Submitter", "Reporter" };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser"),
            new Claim("groups", "/projects/pr74qo/member")
        }));

        SetupMockProjectAndGroup("pr74qo", 10, 20);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.AdaptorUserUserGroupRoles.Should().HaveCount(2);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Submitter && !r.IsDeleted);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Reporter && !r.IsDeleted);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_AssignToAllActiveProjects_AssignsDefaultRolesToAllActiveProjects()
    {
        OpenIdClaimsConfiguration.AssignToAllActiveProjects = true;
        OpenIdClaimsConfiguration.DefaultRoles = new List<string> { "Submitter", "Reporter" };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser")
        }));

        var p1 = new Project { Id = 10, Name = "P1", AccountingString = "P1", StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) };
        var p2 = new Project { Id = 20, Name = "P2", AccountingString = "P2", StartDate = DateTime.UtcNow.AddDays(-1), EndDate = DateTime.UtcNow.AddDays(1) };
        _mockProjectRepo.Setup(r => r.GetAllActiveProjectsAsync()).ReturnsAsync(new List<Project> { p1, p2 });

        var g1 = new AdaptorUserGroup { Id = 101, ProjectId = 10, Name = "G1" };
        var g2 = new AdaptorUserGroup { Id = 102, ProjectId = 20, Name = "G2" };
        _mockGroupRepo.Setup(r => r.GetAllWithAdaptorUserGroupsAndActiveProjectsAsync()).ReturnsAsync(new List<AdaptorUserGroup> { g1, g2 });

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.AdaptorUserUserGroupRoles.Should().HaveCount(4); // 2 projects * 2 default roles
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserGroupId == 101 && r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Submitter);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserGroupId == 101 && r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Reporter);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserGroupId == 102 && r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Submitter);
        user.AdaptorUserUserGroupRoles.Should().Contain(r => r.AdaptorUserGroupId == 102 && r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Reporter);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_OptimisticCheck_WhenRolesUnchanged_SkipsDbSave()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser"),
            new Claim("groups", "/hpda/heappe-pr74qo-Submitter")
        }));

        var (_, group) = SetupMockProjectAndGroup("pr74qo", 10, 20);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };
        existingUser.CreateSpecificUserRoleForUser(group, AdaptorUserRoleType.Submitter);

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        // SaveAsync should NOT be called for roles because roles were already identical
        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenRolesChanged_RevokesOldRoleAndAssignsNewRole()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser"),
            new Claim("groups", "/hpda/heappe-pr74qo-Manager")
        }));

        var (_, group) = SetupMockProjectAndGroup("pr74qo", 10, 20);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };
        // Currently has Submitter
        existingUser.CreateSpecificUserRoleForUser(group, AdaptorUserRoleType.Submitter);

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        // Submitter should now be marked IsDeleted
        var submitterRole = user.AdaptorUserUserGroupRoles.First(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Submitter);
        submitterRole.IsDeleted.Should().BeTrue();

        // Manager should be active
        var managerRole = user.AdaptorUserUserGroupRoles.First(r => r.AdaptorUserRoleId == (long)AdaptorUserRoleType.Manager);
        managerRole.IsDeleted.Should().BeFalse();

        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ResolveAndSyncUserAsync_WhenNoMatchingProjects_ThrowsAuthenticationTypeException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "testuser"),
            new Claim("groups", "/hpda/heappe-unknownProject-Submitter")
        }));

        SetupMockProjectAndGroup("otherProject", 10, 20);

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "testuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("testuser"))
            .ReturnsAsync(existingUser);

        var provider = new OpenIdClaimsUserRoleSourceProvider();
        var act = () => provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        await act.Should().ThrowAsync<AuthenticationTypeException>();
    }

    #endregion

    #region DatabaseUserRoleSourceProvider Tests

    [Fact]
    public async Task DatabaseProvider_WhenUserExistsWithRoles_ReturnsUserWithoutModifyingRoles()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "dbuser")
        }));

        var group = new AdaptorUserGroup { Id = 50, Name = "DbGroup" };
        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "dbuser",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
        };
        existingUser.CreateSpecificUserRoleForUser(group, AdaptorUserRoleType.Submitter);

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("dbuser"))
            .ReturnsAsync(existingUser);

        var provider = new DatabaseUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.Should().BeSameAs(existingUser);
        user.AdaptorUserUserGroupRoles.Should().ContainSingle(r => !r.IsDeleted);
        _mockUnitOfWork.Verify(u => u.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task DatabaseProvider_WhenAllowUnassignedUsersFalseAndNoRoles_ThrowsAuthenticationTypeException()
    {
        DatabaseSourceConfiguration.AllowUnassignedUsers = false;

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "unassigned_user")
        }));

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "unassigned_user",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>() // No roles
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("unassigned_user"))
            .ReturnsAsync(existingUser);

        var provider = new DatabaseUserRoleSourceProvider();
        var act = () => provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        await act.Should().ThrowAsync<AuthenticationTypeException>();
    }

    [Fact]
    public async Task DatabaseProvider_WhenAllowUnassignedUsersTrueAndNoRoles_Succeeds()
    {
        DatabaseSourceConfiguration.AllowUnassignedUsers = true;

        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "unassigned_user")
        }));

        var existingUser = new AdaptorUser
        {
            Id = 1,
            Username = "unassigned_user",
            AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>() // No roles
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("unassigned_user"))
            .ReturnsAsync(existingUser);

        var provider = new DatabaseUserRoleSourceProvider();
        var user = await provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        user.Should().BeSameAs(existingUser);
    }

    [Fact]
    public async Task DatabaseProvider_WhenUserBlocked_ThrowsUnauthorizedAccessException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("preferred_username", "blocked_user")
        }));

        var blockedUser = new AdaptorUser
        {
            Id = 1,
            Username = "blocked_user",
            IsBlocked = true
        };

        _mockUserRepo.Setup(r => r.GetByNameIgnoreQueryFiltersAsync("blocked_user"))
            .ReturnsAsync(blockedUser);

        var provider = new DatabaseUserRoleSourceProvider();
        var act = () => provider.ResolveAndSyncUserAsync(principal, null, _mockUnitOfWork.Object, _mockLogger.Object);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*blocked*");
    }

    #endregion

    private (Project Project, AdaptorUserGroup Group) SetupMockProjectAndGroup(string projectName, long projectId, long groupId)
    {
        var project = new Project
        {
            Id = projectId,
            Name = projectName,
            AccountingString = projectName,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(10)
        };

        var group = new AdaptorUserGroup
        {
            Id = groupId,
            ProjectId = projectId,
            Name = $"Group-{projectName}",
            Project = project
        };

        _mockProjectRepo.Setup(r => r.GetAllActiveProjectsAsync())
            .ReturnsAsync(new List<Project> { project });

        _mockGroupRepo.Setup(r => r.GetAllWithAdaptorUserGroupsAndActiveProjectsAsync())
            .ReturnsAsync(new List<AdaptorUserGroup> { group });

        return (project, group);
    }
}

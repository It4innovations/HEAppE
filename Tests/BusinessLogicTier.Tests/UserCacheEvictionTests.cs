using System;
using System.Collections.Generic;
using FluentAssertions;
using HEAppE.BusinessLogicTier.AuthMiddleware;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.BusinessLogicTier.Logic.Management;
using HEAppE.DataAccessTier.IRepository.JobManagement;
using HEAppE.DataAccessTier.IRepository.Management;
using HEAppE.DataAccessTier.IRepository.UserAndLimitationManagement;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.Management;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.Services.Expirio;
using HEAppE.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using SshCaAPI;
using Xunit;

namespace HEAppE.BusinessLogicTier.Tests;

[Trait("Category", "Unit")]
public class UserCacheEvictionTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<IProjectRepository> _mockProjectRepo = new();
    private readonly Mock<IContactRepository> _mockContactRepo = new();
    private readonly Mock<IAdaptorUserGroupRepository> _mockGroupRepo = new();
    private readonly Mock<IAdaptorUserRepository> _mockUserRepo = new();
    private readonly Mock<ISystemRoleAssignmentRepository> _mockSysRoleRepo = new();
    private readonly Mock<ISshCertificateAuthorityService> _mockSshCa = new();
    private readonly Mock<IHttpContextKeys> _mockHttpContextKeys = new();
    private readonly Mock<IExpirioService> _mockExpirioService = new();
    private readonly Mock<ILogger> _mockLogger = new();

    public UserCacheEvictionTests()
    {
        _mockUnitOfWork.Setup(u => u.ProjectRepository).Returns(_mockProjectRepo.Object);
        _mockUnitOfWork.Setup(u => u.ContactRepository).Returns(_mockContactRepo.Object);
        _mockUnitOfWork.Setup(u => u.AdaptorUserGroupRepository).Returns(_mockGroupRepo.Object);
        _mockUnitOfWork.Setup(u => u.AdaptorUserRepository).Returns(_mockUserRepo.Object);
        _mockUnitOfWork.Setup(u => u.SystemRoleAssignmentRepository).Returns(_mockSysRoleRepo.Object);
        _mockUnitOfWork
            .Setup(u => u.ExecuteExecutionStrategy(It.IsAny<Action>()))
            .Callback<Action>(action => action());
    }

    private ManagementLogic CreateLogic()
    {
        return new ManagementLogic(
            _mockUnitOfWork.Object,
            _mockSshCa.Object,
            _mockHttpContextKeys.Object,
            _mockExpirioService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void CacheUtils_EvictUserCache_RemovesBothUserByIdAndUserByNameCaseInsensitively()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var userIdKey = CacheUtils.GetUserByIdCacheKey(42);
        var userNameKey = CacheUtils.GetUserByNameCacheKey("AdminUser");

        cache.Set(userIdKey, new AdaptorUser { Id = 42, Username = "AdminUser" }, TimeSpan.FromSeconds(10));
        cache.Set(userNameKey, new AdaptorUser { Id = 42, Username = "AdminUser" }, TimeSpan.FromSeconds(10));

        CacheUtils.EvictUserCache(cache, 42, "adminuser");

        cache.TryGetValue(userIdKey, out _).Should().BeFalse();
        cache.TryGetValue(userNameKey, out _).Should().BeFalse();
    }

    [Fact]
    public void CreateProject_EvictsBothUserByIdAndUserByNameCaches()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IMemoryCache))).Returns(cache);

        var previousProvider = LogicFactory.ServiceProvider;
        LogicFactory.ServiceProvider = spMock.Object;

        try
        {
            var user = new AdaptorUser
            {
                Id = 77,
                Username = "ServiceAccount",
                UserType = AdaptorUserType.Default,
                AdaptorUserUserGroupRoles = new List<AdaptorUserUserGroupRole>()
            };

            var userByIdKey = CacheUtils.GetUserByIdCacheKey(user.Id);
            var userByNameKey = CacheUtils.GetUserByNameCacheKey(user.Username);
            cache.Set(userByIdKey, user, TimeSpan.FromSeconds(10));
            cache.Set(userByNameKey, user, TimeSpan.FromSeconds(10));

            _mockProjectRepo.Setup(r => r.GetByAccountingString("ACC-1")).Returns((Project)null!);
            _mockContactRepo.Setup(r => r.GetByEmail("pi@example.com")).Returns(new Contact { Email = "pi@example.com" });
            _mockSysRoleRepo.Setup(r => r.GetAll()).Returns(new List<SystemRoleAssignment>());
            _mockUserRepo.Setup(r => r.GetById(user.Id)).Returns(user);

            var logic = CreateLogic();

            logic.CreateProject(
                "ACC-1",
                UsageType.NodeHours,
                "TestProject",
                "Description",
                DateTime.UtcNow.Date,
                DateTime.UtcNow.Date.AddDays(30),
                false,
                "pi@example.com",
                false,
                user);

            cache.TryGetValue(userByIdKey, out _).Should().BeFalse();
            cache.TryGetValue(userByNameKey, out _).Should().BeFalse();
        }
        finally
        {
            LogicFactory.ServiceProvider = previousProvider;
        }
    }

    [Fact]
    public void SetAdaptorUserBlockStatus_EvictsBothUserByIdAndUserByNameCaches()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var spMock = new Mock<IServiceProvider>();
        spMock.Setup(sp => sp.GetService(typeof(IMemoryCache))).Returns(cache);

        var previousProvider = LogicFactory.ServiceProvider;
        LogicFactory.ServiceProvider = spMock.Object;

        try
        {
            var user = new AdaptorUser
            {
                Id = 88,
                Username = "ApiUser",
                IsBlocked = false
            };

            var userByIdKey = CacheUtils.GetUserByIdCacheKey(user.Id);
            var userByNameKey = CacheUtils.GetUserByNameCacheKey(user.Username);
            cache.Set(userByIdKey, user, TimeSpan.FromSeconds(10));
            cache.Set(userByNameKey, user, TimeSpan.FromSeconds(10));

            _mockUserRepo.Setup(r => r.GetByName("ApiUser")).Returns(user);

            var logic = CreateLogic();
            logic.SetAdaptorUserBlockStatus("ApiUser", true);

            cache.TryGetValue(userByIdKey, out _).Should().BeFalse();
            cache.TryGetValue(userByNameKey, out _).Should().BeFalse();
        }
        finally
        {
            LogicFactory.ServiceProvider = previousProvider;
        }
    }
}

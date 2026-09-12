using FluentAssertions;
using HEAppE.Utils;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HEAppE.RestApi.IntegrationTests.ClusterInformation;

public class ClusterCacheTests
{
    [Fact]
    public void InvalidateClusterCache_IncrementsEpochVersion()
    {
        var initialVersion = CacheUtils.ClusterCacheVersion;
        CacheUtils.InvalidateClusterCache(NullLogger.Instance);
        CacheUtils.ClusterCacheVersion.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void InvalidateClusterCache_DoesNotClearUnrelatedMemoryCacheEntries()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var userSessionKey = "UserSession_123";
        memoryCache.Set(userSessionKey, "active_session_data");

        CacheUtils.InvalidateClusterCache(NullLogger.Instance, memoryCache);

        memoryCache.TryGetValue(userSessionKey, out string? value).Should().BeTrue();
        value.Should().Be("active_session_data");
    }

    [Fact]
    public void InvalidateAllCache_IncrementsClusterCacheVersion()
    {
        var initialVersion = CacheUtils.ClusterCacheVersion;
        CacheUtils.InvalidateAllCache(NullLogger.Instance);
        CacheUtils.ClusterCacheVersion.Should().BeGreaterThan(initialVersion);
    }

    [Fact]
    public void AddClusterInvalidation_DoesNotThrowOrLeak()
    {
        var options = new MemoryCacheEntryOptions();
        
        // Obsolete method should not throw and should not attach tokens
        CacheUtils.AddClusterInvalidation(options);
        options.ExpirationTokens.Should().BeEmpty();
    }

    [Fact]
    public void AdaptorUserProjectClusterUserCache_InvalidateForProject_RemovesEntriesForProjectOnly()
    {
        long project1 = 991, project2 = 992;
        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.SetLastUserId(1, project1, 10, 100, 101);
        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.SetLastUserId(1, project2, 10, 100, 201);

        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.GetLastUserId(1, project1, 10).Should().Be(101);
        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.GetLastUserId(1, project2, 10).Should().Be(201);

        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.InvalidateForProject(project1);

        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.GetLastUserId(1, project1, 10).Should().BeNull();
        HEAppE.BusinessLogicTier.Logic.ClusterInformation.AdaptorUserProjectClusterUserCache.GetLastUserId(1, project2, 10).Should().Be(201);
    }

    [Fact]
    public async Task ConnectionPool_Dispose_ThrowsObjectDisposedException()
    {
        var pool = new HEAppE.ConnectionPool.ConnectionPool(
            "localhost",
            "UTC",
            minSize: 0,
            maxSize: 2,
            maxSessionsPerConnection: 2,
            cleaningInterval: 60,
            maxUnusedDuration: 60,
            adapter: null!,
            retryAttempts: 1,
            timeoutMs: 1000,
            port: 22,
            logger: NullLogger.Instance);

        pool.Dispose();

        var creds = new HEAppE.DomainObjects.ClusterInformation.ClusterAuthenticationCredentials { Id = 1, Username = "test" };
        var cluster = new HEAppE.DomainObjects.ClusterInformation.Cluster();

        Func<Task> act = async () => await pool.GetConnectionForUserAsync(creds, cluster, "", "");
        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void SchedulerFactory_InvalidateForProject_DoesNotThrow()
    {
        Action act = () => HEAppE.HpcConnectionFramework.SchedulerAdapters.SchedulerFactory.InvalidateForProject(12345);
        act.Should().NotThrow();

        Action actAll = () => HEAppE.HpcConnectionFramework.SchedulerAdapters.SchedulerFactory.InvalidateAll();
        actAll.Should().NotThrow();
    }
}

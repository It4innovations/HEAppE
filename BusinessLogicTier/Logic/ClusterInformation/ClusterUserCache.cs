using System.Collections.Concurrent;
using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

/// <summary>
///     Provides thread-safe non-blocking cache of last used user ids for each cluster.
/// </summary>
public static class ClusterUserCache
{
    private static readonly ConcurrentDictionary<long, long> lastUserId = new();

    public static long? GetLastUserId(Cluster cluster)
    {
        if (lastUserId.TryGetValue(cluster.Id, out long id))
        {
            return id;
        }
        return null;
    }

    public static void SetLastUserId(Cluster cluster, ClusterAuthenticationCredentials serviceAccount,
        long clusterUserId)
    {
        if (serviceAccount.Id != clusterUserId)
        {
            lastUserId.AddOrUpdate(cluster.Id, clusterUserId, (key, oldValue) => clusterUserId);
        }
    }
}

public static class AdaptorUserProjectClusterUserCache
{
    private static readonly ConcurrentDictionary<(long, long, long), long> lastUserId = new();

    public static long? GetLastUserId(long adaptorUserId, long projectId, long clusterId)
    {
        var key = (adaptorUserId, projectId, clusterId);
        if (lastUserId.TryGetValue(key, out long id))
        {
            return id;
        }
        return null;
    }

    public static void SetLastUserId(long adaptorUserId, long projectId, long clusterId,
        long serviceAccountId, long clusterUserId)
    {
        if (serviceAccountId != clusterUserId)
        {
            var key = (adaptorUserId, projectId, clusterId);
            lastUserId.AddOrUpdate(key, clusterUserId, (key, oldValue) => clusterUserId);
        }
    }
}
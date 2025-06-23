using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.BusinessLogicTier.Logic.ClusterInformation;

/// <summary>
///     Provides static cache of last used user ids for each cluster.
/// </summary>
public static class ClusterUserCache
{
    private static Dictionary<long, long> lastUserId = new();

    /// <summary>
    ///     Returns id of last used user of given cluster.
    ///     Returns zero if no user of given cluster has been used yet.
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <returns>Last used user id</returns>
    public static long? GetLastUserId(Cluster cluster)
    {
        lock (lastUserId)
        {
            if (lastUserId == null || !lastUserId.ContainsKey(cluster.Id))
                return null;
            return lastUserId[cluster.Id];
        }
    }

    /// <summary>
    ///     Sets last used user id for given cluster.
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="clusterUserId">Last used user id</param>
    public static void SetLastUserId(Cluster cluster, ClusterAuthenticationCredentials serviceAccount,
        long clusterUserId)
    {
        lock (lastUserId)
        {
            if (serviceAccount.Id != clusterUserId)
            {
                lastUserId ??= [];

                if (!lastUserId.ContainsKey(cluster.Id))
                    lastUserId.Add(cluster.Id, clusterUserId);
                else
                    lastUserId[cluster.Id] = clusterUserId;
            }
        }
    }
}

public static class AdaptorUserProjectClusterUserCache
{
    private static Dictionary<(long, long, long), long> lastUserId = [];

    /// <summary>
    ///     Returns id of last used user of given cluster.
    ///     Returns zero if no user of given cluster has been used yet.
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <returns>Last used user id</returns>
    public static long? GetLastUserId(long adaptorUserId, long projectId, long clusterId)
    {
        var key = (adaptorUserId, projectId, clusterId);
        lock (lastUserId)
        {
            if (lastUserId == null || !lastUserId.ContainsKey(key))
                return null;
            return lastUserId[key];
        }
    }

    /// <summary>
    ///     Sets last used user id for given cluster.
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="clusterUserId">Last used user id</param>
    public static void SetLastUserId(long adaptorUserId, long projectId, long clusterId,
        long serviceAccountId, long clusterUserId)
    {
        var key = (adaptorUserId, projectId, clusterId);
        lock (lastUserId)
        {
            if (serviceAccountId != clusterUserId)
            {
                lastUserId ??= [];

                if (!lastUserId.ContainsKey(key))
                    lastUserId.Add(key, clusterUserId);
                else
                    lastUserId[key] = clusterUserId;
            }
        }
    }
}

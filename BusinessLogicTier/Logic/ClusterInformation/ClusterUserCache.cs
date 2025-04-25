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
            if (lastUserId == null || !lastUserId.ContainsKey(cluster.Id)) return null;
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
                if (lastUserId == null) lastUserId = new Dictionary<long, long>();

                if (!lastUserId.ContainsKey(cluster.Id))
                    lastUserId.Add(cluster.Id, clusterUserId);
                else
                    lastUserId[cluster.Id] = clusterUserId;
            }
        }
    }
}
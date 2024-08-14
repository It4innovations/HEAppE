using HEAppE.DomainObjects.ClusterInformation;
using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Timers;
using HEAppE.DataAccessTier.Vault;
using Timer = System.Timers.Timer;

namespace HEAppE.ConnectionPool
{
    public class ConnectionPool : IConnectionPool
    {
        #region Fields
        private readonly IPoolableAdapter adapter;
        private readonly ILog log;
        private readonly string _masterNodeName;
        private readonly int? _port;
        private readonly string _remoteTimeZone;
        private readonly int maxSize;
        private readonly TimeSpan maxUnusedDuration;
        private readonly int minSize;
        private readonly Dictionary<long, LinkedList<ConnectionInfo>> pool;
        private readonly Timer poolCleanTimer;
        private int actualSize;
        #endregion

        #region Constructors
        public ConnectionPool(string masterNodeName, string remoteTimeZone, int minSize, int maxSize, int cleaningInterval, int maxUnusedDuration, IPoolableAdapter adapter, int? port = null)
        {
            this.log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            _masterNodeName = masterNodeName;
            _remoteTimeZone = remoteTimeZone;
            _port = port;
            this.minSize = minSize;
            this.maxSize = maxSize;
            this.adapter = adapter;
            this.pool = new Dictionary<long, LinkedList<ConnectionInfo>>();

            if (cleaningInterval != 0 && maxUnusedDuration != 0)
            {
                poolCleanTimer = new Timer(cleaningInterval * 1000); // conversion from seconds to miliseconds
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                this.maxUnusedDuration = new TimeSpan(maxUnusedDuration * 10000000);
                // conversion from seconds to ticks (100-nanosecond units) 
            }
        }
        #endregion

        #region IConnectionPool Members
        public ConnectionInfo GetConnectionForUser(ClusterAuthenticationCredentials credentials, Cluster cluster)
        {
            //log.DebugFormat("Thread {0} Requesting connectin for {1}",Thread.CurrentThread.ManagedThreadId, credentials.Username);
            ConnectionInfo connection = null;
            do
            {
                lock (pool)
                {
                    if (pool.ContainsKey(credentials.Id) && pool[credentials.Id].Last != null)
                    {
                        // Free connection found
                        connection = pool[credentials.Id].Last.Value;
                        // Remove it from cache
                        pool[credentials.Id].RemoveLast();
                    }
                    else
                    {
                        if (actualSize < maxSize)
                        {
                            // If there is space free, create new connection
                            connection = ExpandPoolAndGetConnection(credentials, cluster);
                        }
                        else if (HasAnyFreeConnection())
                        {
                            // If pool is full, drop oldest connection
                            // Find oldest connection in pool
                            ConnectionInfo oldest = FindOldestConnection();
                            // Drop it
                            RemoveConnectionFromPool(oldest);
                            // Expand pool with newly created one
                            connection = ExpandPoolAndGetConnection(credentials, cluster);
                        }
                        else
                        {
                            // Wait for any returned connection
                            Monitor.Wait(pool);
                        }
                    }
                }
                //log.InfoFormat("Thread {0} Got connection for {1}", Thread.CurrentThread.ManagedThreadId, credentials.Username);
            } while (connection == null);
            return connection;
        }

        public void ReturnConnection(ConnectionInfo connection)
        {
            connection.LastUsed = DateTime.UtcNow;
            lock (pool)
            {
                AddConnectionToPool(connection);
                Monitor.Pulse(pool);
                //log.Debug(String.Format("Connection for user {0} returned.", connection.AuthCredentials.Username));
            }
        }
        #endregion

        #region Local Methods
        private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster)
        {
            if (!cred.IsVaultDataLoaded)
            {
                VaultConnector _vaultConnector = new VaultConnector();
                var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(cred.Id).GetAwaiter().GetResult();
                cred.ImportVaultData(vaultData);
            }
            object connectionObject = adapter.CreateConnectionObject(_masterNodeName, cred, cluster.ProxyConnection, cluster.Port);
            var connection = new ConnectionInfo
            {
                Connection = connectionObject,
                LastUsed = DateTime.UtcNow,
                AuthCredentials = cred
            };
            adapter.Connect(connection.Connection);
            return connection;
        }

        private ConnectionInfo ExpandPoolAndGetConnection(ClusterAuthenticationCredentials cred, Cluster cluster)
        {
            ConnectionInfo connection = InitializeConnection(cred, cluster);
            actualSize++;
            if (poolCleanTimer != null && actualSize > minSize)
                poolCleanTimer.Start();
            log.DebugFormat("Connection pool expanded with acc {0} - actual size {1}", cred.Username, actualSize);
            return connection;
        }

        private void RemoveConnectionFromPool(ConnectionInfo connection)
        {
            pool[connection.AuthCredentials.Id].Remove(connection);
            actualSize--;
            adapter.Disconnect(connection.Connection);
            log.DebugFormat("Removed connection for {0} - actual size {1}", connection.AuthCredentials.Username, actualSize);
        }

        private ConnectionInfo FindOldestConnection()
        {
            ConnectionInfo oldestConnection = null;
            DateTime oldestLastUsedTime = DateTime.UtcNow;
            foreach (var list in pool)
            {
                foreach (var conn in list.Value)
                {
                    if (DateTime.Compare(oldestLastUsedTime, conn.LastUsed) >= 0)
                    {
                        oldestConnection = conn;
                        oldestLastUsedTime = conn.LastUsed;
                    }
                }
            }
            return oldestConnection;
        }

        private bool HasAnyFreeConnection()
        {
            bool hasConnection = false;
            foreach (var item in pool)
            {
                if (item.Value.Count > 0)
                {
                    hasConnection = true;
                    break;
                }
            }
            return hasConnection;
        }

        private void AddConnectionToPool(ConnectionInfo connection)
        {
            if (!pool.ContainsKey(connection.AuthCredentials.Id))
            {
                pool.Add(connection.AuthCredentials.Id, new LinkedList<ConnectionInfo>());
            }
            pool[connection.AuthCredentials.Id].AddLast(connection);
        }
        #endregion

        #region Timer Event Handlers
        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            log.Debug("Connection pool clean start.");
            lock (pool)
            {
                foreach (var connectionList in pool)
                {
                    LinkedListNode<ConnectionInfo> connectionNode = connectionList.Value.First;
                    while (connectionNode != null)
                    {
                        ConnectionInfo connection = connectionNode.Value;
                        if (connection.LastUsed < DateTime.UtcNow.Subtract(maxUnusedDuration))
                        {
                            RemoveConnectionFromPool(connection);
                        }
                        if (actualSize == minSize)
                        {
                            poolCleanTimer.Stop();
                            break;
                        }
                        connectionNode = connectionNode.Next;
                    }
                }
            }
        }
        #endregion
    }
}
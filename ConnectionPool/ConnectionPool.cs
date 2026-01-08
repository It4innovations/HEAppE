using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Timers;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using log4net;
using Timer = System.Timers.Timer;

namespace HEAppE.ConnectionPool
{
    public class ConnectionPool : IConnectionPool
    {
        #region Inner Classes

        /// <summary>
        /// Encapsulates a shared physical connection and tracks its active usage references.
        /// </summary>
        private class SharedConnectionContext
        {
            /// <summary>
            /// The underlying physical connection object.
            /// </summary>
            public ConnectionInfo ConnectionInfo { get; set; }

            /// <summary>
            /// The number of threads currently holding a reference to this connection.
            /// </summary>
            public int ReferenceCount = 0;

            /// <summary>
            /// The timestamp when the ReferenceCount last dropped to zero.
            /// Used to determine if the connection should be cleaned up.
            /// </summary>
            public DateTime LastReleasedTime = DateTime.UtcNow;

            /// <summary>
            /// Synchronization object for thread-safe access to this context.
            /// </summary>
            public readonly object SyncRoot = new object();
        }

        #endregion

        #region Fields

        private readonly IPoolableAdapter adapter;
        private readonly ILog log;
        private readonly string _masterNodeName;
        private readonly int? _port;
        private readonly string _remoteTimeZone;

        // Configuration
        private readonly int minSize;
        private readonly int maxSize;
        private readonly TimeSpan maxUnusedDuration;

        // State
        private int _currentPhysicalConnectionsCount;
        private readonly SemaphoreSlim _semaphore;
        
        // Dictionary mapping UserID to their shared connection context.
        private readonly ConcurrentDictionary<long, SharedConnectionContext> _sharedPool;
        
        private readonly Timer poolCleanTimer;

        #endregion

        #region Constructors

        public ConnectionPool(
            string masterNodeName,
            string remoteTimeZone,
            int minSize,
            int maxSize,
            int cleaningInterval,
            int maxUnusedDuration,
            IPoolableAdapter adapter,
            int? port)
        {
            log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            _masterNodeName = masterNodeName;
            _remoteTimeZone = remoteTimeZone;
            _port = port;
            this.minSize = minSize;
            this.maxSize = maxSize;
            this.adapter = adapter;

            _sharedPool = new ConcurrentDictionary<long, SharedConnectionContext>();

            // The semaphore limits the total number of UNIQUE physical connections (active users).
            // It does not limit the number of reused references per user.
            _semaphore = new SemaphoreSlim(maxSize, maxSize);

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false; // Prevent re-entry if cleaning takes longer than the interval
                this.maxUnusedDuration = TimeSpan.FromSeconds(maxUnusedDuration);
            }
        }

        #endregion

        #region IConnectionPool Members

        public ConnectionInfo GetConnectionForUser(
            ClusterAuthenticationCredentials credentials,
            Cluster cluster,
            string sshCaToken)
        {
            // Atomically retrieve or create the context for the user ID.
            var context = _sharedPool.GetOrAdd(credentials.Id, _ => new SharedConnectionContext());

            lock (context.SyncRoot)
            {
                // 1. If the physical connection does not exist within this context, initialize it.
                if (context.ConnectionInfo == null)
                {
                    // Acquire semaphore to ensure we do not exceed the global maximum of physical connections.
                    _semaphore.Wait();

                    try
                    {
                        var newConnection = InitializeConnection(credentials, cluster, sshCaToken);
                        context.ConnectionInfo = newConnection;

                        Interlocked.Increment(ref _currentPhysicalConnectionsCount);

                        // Ensure the cleanup timer is running if we are above the minimum size.
                        if (poolCleanTimer != null && !poolCleanTimer.Enabled && _currentPhysicalConnectionsCount > minSize)
                        {
                            poolCleanTimer.Start();
                        }
                    }
                    catch
                    {
                        // Release the semaphore if initialization fails.
                        _semaphore.Release();
                        // Remove the invalid context to allow a fresh retry next time.
                        _sharedPool.TryRemove(credentials.Id, out _);
                        throw;
                    }
                }

                // 2. Increment the usage counter (Reference Counting).
                // Multiple threads can share this single physical connection.
                context.ReferenceCount++;
                
                // Update the LastUsed property for informational purposes.
                context.ConnectionInfo.LastUsed = DateTime.UtcNow;

                return context.ConnectionInfo;
            }
        }

        public void ReturnConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            if (_sharedPool.TryGetValue(connection.AuthCredentials.Id, out var context))
            {
                lock (context.SyncRoot)
                {
                    // Decrement the active usage counter.
                    context.ReferenceCount--;

                    // Update timestamp.
                    connection.LastUsed = DateTime.UtcNow;

                    if (context.ReferenceCount <= 0)
                    {
                        // No active references remain.
                        // Mark the time when it became idle. The physical connection remains open ("warm")
                        // until the cleanup timer determines it has been unused for too long.
                        context.ReferenceCount = 0; 
                        context.LastReleasedTime = DateTime.UtcNow;
                    }
                }
            }
            else
            {
                log.Warn($"Attempted to return a connection for user {connection.AuthCredentials.Username}, but the context was not found in the pool.");
            }
        }

        #endregion

        #region Timer Event Handlers

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                log.Debug("Connection pool cleanup started.");

                foreach (var entry in _sharedPool)
                {
                    var userId = entry.Key;
                    var context = entry.Value;
                    bool shouldRemove = false;

                    // Inspect the context state in a thread-safe manner.
                    lock (context.SyncRoot)
                    {
                        var idleDuration = DateTime.UtcNow - context.LastReleasedTime;

                        // Condition to close the connection:
                        // 1. No active references (ReferenceCount == 0).
                        // 2. The connection has been idle longer than allowed (maxUnusedDuration).
                        // 3. We are above the minimum pool size.
                        if (context.ReferenceCount == 0 &&
                            idleDuration > maxUnusedDuration &&
                            _currentPhysicalConnectionsCount > minSize)
                        {
                            shouldRemove = true;
                        }
                    }

                    if (shouldRemove)
                    {
                        // Attempt to remove the context from the dictionary.
                        // If successful, we assume ownership of the context cleanup.
                        if (_sharedPool.TryRemove(userId, out var removedContext))
                        {
                            // Double-check lock to ensure no race condition occurred during removal 
                            // (e.g., a thread acquiring it right before removal).
                            lock (removedContext.SyncRoot)
                            {
                                if (removedContext.ReferenceCount == 0)
                                {
                                    RemovePhysicalConnection(removedContext.ConnectionInfo);
                                }
                                else
                                {
                                    // Edge case: A race condition occurred, and the connection became active again 
                                    // immediately before TryRemove. Since it is removed from the map, 
                                    // we treat it as an isolated instance and close it to avoid leaks, 
                                    // or we could log a warning. Closing is safer for consistency.
                                    RemovePhysicalConnection(removedContext.ConnectionInfo);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error during connection pool cleanup.", ex);
            }
            finally
            {
                // Restart the timer if necessary.
                if (poolCleanTimer != null && _currentPhysicalConnectionsCount > minSize)
                {
                    poolCleanTimer.Start();
                }
            }
        }

        #endregion

        #region Local Methods

        private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
        {
            // Sync-over-async call (GetResult) should be replaced with async/await if the interface allows it in the future.
            if (!cred.IsVaultDataLoaded)
            {
                var _vaultConnector = new VaultConnector();
                var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(cred.Id).GetAwaiter().GetResult();
                cred.ImportVaultData(vaultData);
            }

            var connectionObject = adapter.CreateConnectionObject(_masterNodeName, cred, cluster.ProxyConnection, sshCaToken, cluster.Port);

            var connection = new ConnectionInfo
            {
                Connection = connectionObject,
                LastUsed = DateTime.UtcNow,
                AuthCredentials = cred
            };

            adapter.Connect(connection.Connection);
            return connection;
        }

        private void RemovePhysicalConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            try
            {
                // Physically close the connection.
                adapter.Disconnect(connection.Connection);
            }
            catch (Exception ex)
            {
                log.Warn($"Error disconnecting connection for {connection.AuthCredentials.Username}", ex);
            }
            finally
            {
                // 1. Decrement the global physical connection counter.
                Interlocked.Decrement(ref _currentPhysicalConnectionsCount);

                // 2. Release the semaphore slot to allow new physical connections.
                _semaphore.Release();

                log.DebugFormat(
                    "Removed physical connection for {0} - actual size {1}",
                    connection.AuthCredentials.Username,
                    _currentPhysicalConnectionsCount);
            }
        }

        #endregion
    }
}
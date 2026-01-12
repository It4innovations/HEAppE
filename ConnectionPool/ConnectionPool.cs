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
        #region Constants & Configuration

        // OPTIMIZATION: Sharding Factor.
        // Determines how many physical SSH connections allow per user.
        // Increasing this allows parallel execution for a single user (e.g., submitting 10 jobs at once).
        private const int MaxConnectionsPerUser = 2; 

        #endregion

        #region Inner Classes

        /// <summary>
        /// Represents a specific "slot" for a physical connection.
        /// Each user has multiple slots (defined by MaxConnectionsPerUser).
        /// </summary>
        private class ConnectionSlot
        {
            public ConnectionInfo ConnectionInfo { get; set; }
            
            // Counts how many threads are currently using this specific connection.
            public int ReferenceCount = 0;
            
            // Timestamp when the reference count dropped to zero.
            public DateTime LastReleasedTime = DateTime.UtcNow;
            
            // Lock for thread-safety within this specific slot.
            public readonly object SyncRoot = new object();
        }

        /// <summary>
        /// Container for all connection slots belonging to a specific user.
        /// </summary>
        private class SharedUserContext
        {
            public readonly ConnectionSlot[] Slots;
            private int _roundRobinCounter = 0;

            public SharedUserContext(int capacity)
            {
                Slots = new ConnectionSlot[capacity];
                for (int i = 0; i < capacity; i++)
                {
                    Slots[i] = new ConnectionSlot();
                }
            }

            /// <summary>
            /// Returns the next slot in a round-robin fashion to distribute load.
            /// </summary>
            public ConnectionSlot GetNextSlot()
            {
                // Simple thread-safe round-robin selection
                var idx = (Interlocked.Increment(ref _roundRobinCounter) & 0x7FFFFFFF) % Slots.Length;
                return Slots[idx];
            }
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
        
        // Limits the TOTAL number of physical connections to the HPC across all users.
        private readonly SemaphoreSlim _semaphore;
        
        // Stores the context (slots) for each user.
        private readonly ConcurrentDictionary<long, SharedUserContext> _userContexts;
        
        // OPTIMIZATION: Cache for Vault credentials to avoid repeated HTTP requests.
        private static readonly ConcurrentDictionary<long, ClusterProjectCredentialVaultPart> _vaultCache 
            = new ConcurrentDictionary<long, ClusterProjectCredentialVaultPart>();
        
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

            _userContexts = new ConcurrentDictionary<long, SharedUserContext>();
            _semaphore = new SemaphoreSlim(maxSize, maxSize);

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false; 
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
            // 1. Get the container for the user (create if not exists).
            var userContext = _userContexts.GetOrAdd(credentials.Id, _ => new SharedUserContext(MaxConnectionsPerUser));

            // 2. Select a slot using Round-Robin.
            // This distributes the user's workload across multiple physical SSH tunnels.
            var slot = userContext.GetNextSlot();

            // 3. OPTIMIZATION: Fast path check (Double-Checked Locking).
            // If the connection is already open, grab it quickly without heavy logic.
            lock (slot.SyncRoot)
            {
                if (slot.ConnectionInfo != null && adapter.IsConnected(slot.ConnectionInfo.Connection))
                {
                    slot.ReferenceCount++;
                    slot.ConnectionInfo.LastUsed = DateTime.UtcNow;
                    return slot.ConnectionInfo;
                }
            }

            // 4. OPTIMIZATION: Prepare Vault data OUTSIDE the lock.
            // Fetching from Vault is an HTTP call (~200ms). We don't want to block other threads while waiting for this.
            EnsureVaultDataLoaded(credentials);

            // 5. Critical Section: Initialize the connection.
            lock (slot.SyncRoot)
            {
                // Re-check: Another thread might have initialized this slot while we were fetching Vault data.
                if (slot.ConnectionInfo != null && adapter.IsConnected(slot.ConnectionInfo.Connection))
                {
                    slot.ReferenceCount++;
                    slot.ConnectionInfo.LastUsed = DateTime.UtcNow;
                    return slot.ConnectionInfo;
                }

                // We are about to open a new physical connection.
                // Wait for the semaphore to ensure we don't exceed global limits (maxSize).
                _semaphore.Wait();

                try
                {
                    log.Debug($"Initializing new SSH connection for user {credentials.Username} (Slot ID: {slot.GetHashCode()})...");

                    // Create and Connect (The heavy 5s operation).
                    var newConnection = InitializeConnection(credentials, cluster, sshCaToken);
                    slot.ConnectionInfo = newConnection;

                    Interlocked.Increment(ref _currentPhysicalConnectionsCount);

                    // Ensure cleanup timer is running.
                    if (poolCleanTimer != null && !poolCleanTimer.Enabled && _currentPhysicalConnectionsCount > minSize)
                    {
                        poolCleanTimer.Start();
                    }
                }
                catch (Exception ex)
                {
                    log.Error("Failed to initialize connection", ex);
                    _semaphore.Release(); // Always release semaphore on failure.
                    throw;
                }

                slot.ReferenceCount++;
                return slot.ConnectionInfo;
            }
        }

        public void ReturnConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            // Find the user context
            if (_userContexts.TryGetValue(connection.AuthCredentials.Id, out var userContext))
            {
                // We need to find which slot this connection belongs to.
                // Since MaxConnectionsPerUser is small (e.g., 2-4), iteration is very fast.
                foreach (var slot in userContext.Slots)
                {
                    lock (slot.SyncRoot)
                    {
                        // Check by reference equality
                        if (slot.ConnectionInfo == connection)
                        {
                            slot.ReferenceCount--;
                            connection.LastUsed = DateTime.UtcNow;

                            if (slot.ReferenceCount <= 0)
                            {
                                slot.ReferenceCount = 0; // Safety clamp
                                slot.LastReleasedTime = DateTime.UtcNow;
                            }
                            return; // Found and handled
                        }
                    }
                }
            }
            else
            {
                log.Warn($"Attempted to return a connection for user {connection.AuthCredentials.Username}, but the context was not found.");
            }
        }

        #endregion

        #region Timer & Cleanup

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                log.Debug("Connection pool cleanup started.");

                foreach (var userEntry in _userContexts)
                {
                    var userContext = userEntry.Value;

                    // Iterate over all slots for this user
                    foreach (var slot in userContext.Slots)
                    {
                        bool shouldRemove = false;
                        ConnectionInfo connToRemove = null;

                        lock (slot.SyncRoot)
                        {
                            if (slot.ConnectionInfo == null) continue;

                            var idleDuration = DateTime.UtcNow - slot.LastReleasedTime;

                            // Cleanup Condition:
                            // 1. No active refs.
                            // 2. Idle for too long.
                            // 3. Pool is larger than minSize.
                            if (slot.ReferenceCount == 0 &&
                                idleDuration > maxUnusedDuration &&
                                _currentPhysicalConnectionsCount > minSize)
                            {
                                shouldRemove = true;
                                connToRemove = slot.ConnectionInfo;
                                
                                // Detach connection from the slot immediately
                                slot.ConnectionInfo = null;
                            }
                        }

                        if (shouldRemove && connToRemove != null)
                        {
                            RemovePhysicalConnection(connToRemove);
                        }
                    }
                    
                    // Note: We are keeping the UserContext object alive even if slots are empty.
                    // This is fine as it's lightweight. Only physical connections are heavy.
                }
            }
            catch (Exception ex)
            {
                log.Error("Error during connection pool cleanup.", ex);
            }
            finally
            {
                if (poolCleanTimer != null && _currentPhysicalConnectionsCount > minSize)
                {
                    poolCleanTimer.Start();
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// OPTIMIZATION: Ensures Vault data is loaded. Uses an internal cache to avoid 
        /// redundant HTTP calls to the Vault service during the connection phase.
        /// </summary>
        private void EnsureVaultDataLoaded(ClusterAuthenticationCredentials cred)
        {
            if (cred.IsVaultDataLoaded) return;

            // 1. Zkusíme vytáhnout data z Cache
            if (_vaultCache.TryGetValue(cred.Id, out ClusterProjectCredentialVaultPart cachedData))
            {
                cred.ImportVaultData(cachedData);
                return;
            }

            // 2. Pokud nejsou v cache, stáhneme je z Vaultu
            var _vaultConnector = new VaultConnector();
    
            // Předpokládám, že tato metoda vrací ClusterProjectCredentialVaultPart
            var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(cred.Id).GetAwaiter().GetResult();
    
            // 3. Importujeme data do credentials objektu
            cred.ImportVaultData(vaultData);
    
            // 4. Uložíme do cache pro příště
            _vaultCache.TryAdd(cred.Id, vaultData);
        }

        private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
        {
            // Create the adapter object (lightweight)
            var connectionObject = adapter.CreateConnectionObject(
                _masterNodeName, 
                cred, 
                cluster.ProxyConnection, 
                sshCaToken, 
                cluster.Port
            );

            var connection = new ConnectionInfo
            {
                Connection = connectionObject,
                LastUsed = DateTime.UtcNow,
                AuthCredentials = cred
            };

            // Connect (Heavyweight - network I/O)
            adapter.Connect(connection.Connection);
            
            return connection;
        }

        private void RemovePhysicalConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            try
            {
                adapter.Disconnect(connection.Connection);
            }
            catch (Exception ex)
            {
                log.Warn($"Error disconnecting connection for {connection.AuthCredentials.Username}", ex);
            }
            finally
            {
                Interlocked.Decrement(ref _currentPhysicalConnectionsCount);
                _semaphore.Release();

                log.DebugFormat(
                    "Removed physical connection for {0}. Active connections: {1}",
                    connection.AuthCredentials.Username,
                    _currentPhysicalConnectionsCount);
            }
        }

        #endregion
    }
}
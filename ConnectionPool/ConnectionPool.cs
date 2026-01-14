using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using log4net;
using Timer = System.Timers.Timer;

namespace HEAppE.ConnectionPool
{
    public class ConnectionPool : IConnectionPool
    {
        private class ConnectionSlot
        {
            public ConnectionInfo ConnectionInfo { get; set; }
            public int ReferenceCount = 0;
            public DateTime LastReleasedTime = DateTime.UtcNow;
            public readonly object SyncRoot = new object();
        }

        private class SharedUserContext
        {
            public readonly ConnectionSlot[] Slots;
            public readonly SemaphoreSlim UserSemaphore;
            private int _roundRobinCounter = 0;

            public SharedUserContext(int capacity)
            {
                Slots = new ConnectionSlot[capacity];
                for (int i = 0; i < capacity; i++) Slots[i] = new ConnectionSlot();
                // Enforces limit per Credential ID
                UserSemaphore = new SemaphoreSlim(capacity, capacity);
            }

            public ConnectionSlot GetNextSlot()
            {
                var idx = (Interlocked.Increment(ref _roundRobinCounter) & 0x7FFFFFFF) % Slots.Length;
                return Slots[idx];
            }
        }

        private readonly IPoolableAdapter adapter;
        private readonly ILog log;
        private readonly string _masterNodeName;
        private readonly int? _port;
        private readonly int _maxConnectionsPerUser;
        private readonly int minSize;
        private readonly TimeSpan maxUnusedDuration;
        private int _currentTotalPhysicalConnectionsCount;
        
        private readonly ConcurrentDictionary<long, SharedUserContext> _userContexts;
        
        // Cache tasks to handle parallel Vault requests efficiently
        private static readonly ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>> _vaultCache 
            = new ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>>();
            
        private readonly Timer poolCleanTimer;

        public ConnectionPool(string masterNodeName, string remoteTimeZone, int minSize, int maxSize, int cleaningInterval, int maxUnusedDuration, IPoolableAdapter adapter, int? port)
        {
            log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            _masterNodeName = masterNodeName;
            _port = port;
            this.minSize = minSize;
            _maxConnectionsPerUser = maxSize; 
            this.adapter = adapter;
            _userContexts = new ConcurrentDictionary<long, SharedUserContext>();

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                this.maxUnusedDuration = TimeSpan.FromSeconds(maxUnusedDuration);
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false;
            }
        }

        public ConnectionInfo GetConnectionForUser(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken)
        {
            // Fulfills the synchronous interface while running internal logic asynchronously
            return Task.Run(async () => await GetConnectionForUserInternalAsync(credentials, cluster, sshCaToken))
                       .GetAwaiter()
                       .GetResult();
        }

        private async Task<ConnectionInfo> GetConnectionForUserInternalAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken)
        {
            var userContext = _userContexts.GetOrAdd(credentials.Id, id => new SharedUserContext(_maxConnectionsPerUser));

            // Fast path: reuse existing active connection
            foreach (var s in userContext.Slots)
            {
                lock (s.SyncRoot)
                {
                    if (s.ConnectionInfo != null && adapter.IsConnected(s.ConnectionInfo.Connection))
                    {
                        s.ReferenceCount++;
                        s.ConnectionInfo.LastUsed = DateTime.UtcNow;
                        return s.ConnectionInfo;
                    }
                }
            }

            // Await vault data asynchronously to avoid thread starvation
            await EnsureVaultDataLoadedAsync(credentials);

            // Wait for user-specific semaphore non-blockingly
            await userContext.UserSemaphore.WaitAsync();
            try
            {
                var slot = userContext.GetNextSlot();
                lock (slot.SyncRoot)
                {
                    // Re-check status after acquiring semaphore and lock
                    if (slot.ConnectionInfo != null && adapter.IsConnected(slot.ConnectionInfo.Connection))
                    {
                        userContext.UserSemaphore.Release();
                        slot.ReferenceCount++;
                        return slot.ConnectionInfo;
                    }

                    var newConnection = InitializeConnection(credentials, cluster, sshCaToken);
                    slot.ConnectionInfo = newConnection;
                    
                    Interlocked.Increment(ref _currentTotalPhysicalConnectionsCount);
                    if (poolCleanTimer != null && !poolCleanTimer.Enabled && _currentTotalPhysicalConnectionsCount > minSize)
                    {
                        poolCleanTimer.Start();
                    }

                    slot.ReferenceCount++;
                    return slot.ConnectionInfo;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Connection setup failed for user {credentials.Id}", ex);
                userContext.UserSemaphore.Release();
                throw;
            }
        }

        public void ReturnConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            if (_userContexts.TryGetValue(connection.AuthCredentials.Id, out var userContext))
            {
                foreach (var slot in userContext.Slots)
                {
                    lock (slot.SyncRoot)
                    {
                        if (slot.ConnectionInfo == connection)
                        {
                            slot.ReferenceCount--;
                            connection.LastUsed = DateTime.UtcNow;
                            if (slot.ReferenceCount <= 0)
                            {
                                slot.ReferenceCount = 0;
                                slot.LastReleasedTime = DateTime.UtcNow;
                            }
                            return;
                        }
                    }
                }
            }
        }

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var userEntry in _userContexts)
                {
                    var userContext = userEntry.Value;
                    foreach (var slot in userContext.Slots)
                    {
                        ConnectionInfo connToRemove = null;
                        lock (slot.SyncRoot)
                        {
                            if (slot.ConnectionInfo == null) continue;
                            if (slot.ReferenceCount == 0 && (DateTime.UtcNow - slot.LastReleasedTime) > maxUnusedDuration && _currentTotalPhysicalConnectionsCount > minSize)
                            {
                                connToRemove = slot.ConnectionInfo;
                                slot.ConnectionInfo = null;
                            }
                        }
                        if (connToRemove != null) RemovePhysicalConnection(connToRemove, userContext);
                    }
                }
            }
            catch (Exception ex) { log.Error("Pool cleanup error", ex); }
            finally
            {
                if (poolCleanTimer != null && _currentTotalPhysicalConnectionsCount > minSize) poolCleanTimer.Start();
            }
        }

        private async Task EnsureVaultDataLoadedAsync(ClusterAuthenticationCredentials cred)
        {
            if (cred.IsVaultDataLoaded) return;
            
            var vaultTask = _vaultCache.GetOrAdd(cred.Id, async id =>
            {
                var connector = new VaultConnector();
                return await connector.GetClusterAuthenticationCredentials(id);
            });
            
            try 
            {
                var vaultData = await vaultTask;
                cred.ImportVaultData(vaultData);
            }
            catch 
            {
                _vaultCache.TryRemove(cred.Id, out _);
                throw;
            }
        }

        private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
        {
            var connectionObject = adapter.CreateConnectionObject(_masterNodeName, cred, cluster.ProxyConnection, sshCaToken, cluster.Port ?? _port);
            var connection = new ConnectionInfo { Connection = connectionObject, LastUsed = DateTime.UtcNow, AuthCredentials = cred };
            adapter.Connect(connection.Connection);
            return connection;
        }

        private void RemovePhysicalConnection(ConnectionInfo connection, SharedUserContext context)
        {
            try { adapter.Disconnect(connection.Connection); }
            finally
            {
                Interlocked.Decrement(ref _currentTotalPhysicalConnectionsCount);
                context.UserSemaphore.Release();
            }
        }
    }
}
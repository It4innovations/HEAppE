using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using log4net;
using Renci.SshNet;
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
        private static readonly ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>> _vaultCache 
            = new ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>>();
        private readonly int _connectionRetryAttempts = 3;
        private readonly int _connectionTimeoutMs =  30000;
            
        private readonly Timer poolCleanTimer;

        public ConnectionPool(string masterNodeName, string remoteTimeZone, int minSize, int maxSize, int cleaningInterval, int maxUnusedDuration, IPoolableAdapter adapter, int retryAttempts, int timeoutMs, int? port)
        {
            log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            _masterNodeName = masterNodeName;
            _port = port;
            this.minSize = minSize;
            _maxConnectionsPerUser = maxSize; 
            this.adapter = adapter;
            _userContexts = new ConcurrentDictionary<long, SharedUserContext>();
            _connectionRetryAttempts = retryAttempts;
            _connectionTimeoutMs = timeoutMs;

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                this.maxUnusedDuration = TimeSpan.FromSeconds(maxUnusedDuration);
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false;
                log.Debug($"ConnectionPool initialized. Cleaning interval: {cleaningInterval}s, Max unused: {maxUnusedDuration}s");
            }
        }

        public ConnectionInfo GetConnectionForUser(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken)
        {
            return Task.Run(async () => await GetConnectionForUserInternalAsync(credentials, cluster, sshCaToken))
                       .GetAwaiter()
                       .GetResult();
        }

        private async Task<ConnectionInfo> GetConnectionForUserInternalAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken)
        {
            log.Debug($"[User:{credentials.Id}] Requesting connection.");
            var userContext = _userContexts.GetOrAdd(credentials.Id, id => {
                log.Debug($"[User:{id}] Creating new SharedUserContext with capacity {_maxConnectionsPerUser}");
                return new SharedUserContext(_maxConnectionsPerUser);
            });

            // Fast path: reuse existing active connection
            for (int i = 0; i < userContext.Slots.Length; i++)
            {
                var s = userContext.Slots[i];
                lock (s.SyncRoot)
                {
                    if (s.ConnectionInfo != null && adapter.IsConnected(s.ConnectionInfo.Connection))
                    {
                        s.ReferenceCount++;
                        s.ConnectionInfo.LastUsed = DateTime.UtcNow;
                        log.Debug($"[User:{credentials.Id}] Reusing existing connection from slot {i}. RefCount: {s.ReferenceCount}");
                        return s.ConnectionInfo;
                    }
                }
            }

            log.Debug($"[User:{credentials.Id}] No idle connection found. Waiting for slot semaphore (Available: {userContext.UserSemaphore.CurrentCount})...");
            
            await EnsureVaultDataLoadedAsync(credentials);
            await userContext.UserSemaphore.WaitAsync();

            try
            {
                var slot = userContext.GetNextSlot();
                lock (slot.SyncRoot)
                {
                    if (slot.ConnectionInfo != null && adapter.IsConnected(slot.ConnectionInfo.Connection))
                    {
                        log.Debug($"[User:{credentials.Id}] Slot became available with active connection during wait.");
                        userContext.UserSemaphore.Release();
                        slot.ReferenceCount++;
                        return slot.ConnectionInfo;
                    }

                    log.Debug($"[User:{credentials.Id}] Initializing new physical connection. Total connections: {_currentTotalPhysicalConnectionsCount + 1}");
                    var newConnection = InitializeConnection(credentials, cluster, sshCaToken);
                    slot.ConnectionInfo = newConnection;
                    
                    Interlocked.Increment(ref _currentTotalPhysicalConnectionsCount);
                    if (poolCleanTimer != null && !poolCleanTimer.Enabled && _currentTotalPhysicalConnectionsCount > minSize)
                    {
                        log.Debug("Starting cleanup timer.");
                        poolCleanTimer.Start();
                    }

                    slot.ReferenceCount++;
                    return slot.ConnectionInfo;
                }
            }
            catch (Exception ex)
            {
                log.Error($"[User:{credentials.Id}] Connection setup failed", ex);
                userContext.UserSemaphore.Release();
                throw;
            }
        }

        public void ReturnConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            if (_userContexts.TryGetValue(connection.AuthCredentials.Id, out var userContext))
            {
                for (int i = 0; i < userContext.Slots.Length; i++)
                {
                    var slot = userContext.Slots[i];
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
                            log.Debug($"[User:{connection.AuthCredentials.Id}] Connection returned to slot {i}. RefCount: {slot.ReferenceCount}");
                            return;
                        }
                    }
                }
            }
            log.Warn($"[User:{connection.AuthCredentials.Id}] Attempted to return a connection that is not managed by this pool.");
        }

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // clear logging thread properties
            LogicalThreadContext.Properties.Clear();

            log.Debug($"Cleanup cycle started. Current physical connections: {_currentTotalPhysicalConnectionsCount}");
            int closedCount = 0;
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
                            
                            bool isExpired = (DateTime.UtcNow - slot.LastReleasedTime) > maxUnusedDuration;
                            log.Debug($"[User:{userEntry.Key}] Checking slot. RefCount: {slot.ReferenceCount}, LastReleased: {slot.LastReleasedTime}, IsExpired: {isExpired}, will expire in: {(slot.LastReleasedTime + maxUnusedDuration) - DateTime.UtcNow}");
                            if (slot.ReferenceCount == 0 && isExpired && _currentTotalPhysicalConnectionsCount > minSize)
                            {
                                connToRemove = slot.ConnectionInfo;
                                slot.ConnectionInfo = null;
                            }
                        }

                        if (connToRemove != null)
                        {
                            log.Debug($"[User:{userEntry.Key}] Closing idle expired connection.");
                            RemovePhysicalConnection(connToRemove, userContext);
                            closedCount++;
                        }
                    }
                }
            }
            catch (Exception ex) { log.Error("Pool cleanup error", ex); }
            finally
            {
                if (closedCount > 0) log.Debug($"Cleanup finished. Closed {closedCount} connections.");
                if (poolCleanTimer != null && _currentTotalPhysicalConnectionsCount > minSize)
                {
                    poolCleanTimer.Start();
                }
                else
                {
                    log.Debug("Cleanup timer stopped (pool at or below minSize).");
                }
            }
        }

        private async Task EnsureVaultDataLoadedAsync(ClusterAuthenticationCredentials cred)
        {
            if (cred.IsVaultDataLoaded) return;
            
            log.Debug($"[User:{cred.Id}] Loading vault data...");
            var vaultTask = _vaultCache.GetOrAdd(cred.Id, async id =>
            {
                log.Debug($"[User:{id}] Fetching vault data from service (Shared Task).");
                var connector = new VaultConnector();
                return await connector.GetClusterAuthenticationCredentials(id);
            });
            
            try 
            {
                var vaultData = await vaultTask;
                cred.ImportVaultData(vaultData);
            }
            catch (Exception ex)
            {
                log.Error($"[User:{cred.Id}] Failed to load vault data", ex);
                _vaultCache.TryRemove(cred.Id, out _);
                throw;
            }
        }

        private void RemovePhysicalConnection(ConnectionInfo connection, SharedUserContext context)
        {
            try 
            { 
                adapter.Disconnect(connection.Connection); 
            }
            catch (Exception ex)
            {
                log.Warn($"Error while disconnecting connection for user {connection.AuthCredentials.Id}", ex);
            }
            finally
            {
                Interlocked.Decrement(ref _currentTotalPhysicalConnectionsCount);
                context.UserSemaphore.Release();
                log.Debug($"[User:{connection.AuthCredentials.Id}] Physical connection removed. Semaphore released.");
            }
        }
        
        private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
        {
            var connectionObject = adapter.CreateConnectionObject(_masterNodeName, cred, cluster.ProxyConnection, sshCaToken, cluster.Port ?? _port);
            var connection = new ConnectionInfo { Connection = connectionObject, LastUsed = DateTime.UtcNow, AuthCredentials = cred };
            
            var username = connection.AuthCredentials.Username;
            
            if (connectionObject is SshClient sshClient)
            {
                username = sshClient.ConnectionInfo.Username;
                sshClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs);
            }
            else if (connectionObject is ScpClient scpClient)
            {
                username = scpClient.ConnectionInfo.Username;
                scpClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs);
            }else if (connectionObject is SftpClient sftpClient)
            {
                username = sftpClient.ConnectionInfo.Username;
                sftpClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs);
            }

            int maxRetries = _connectionRetryAttempts;
            int currentAttempt = 0;

            log.Info($"[User:({connection.AuthCredentials.Id},{username})] Initializing connection. Max retries: {maxRetries}, Timeout: {_connectionTimeoutMs}ms");

            while (true)
            {
                try
                {
                    adapter.Connect(connection.Connection);
                    log.Info($"[User:({connection.AuthCredentials.Id},{username})] Connection initialized successfully on attempt {currentAttempt + 1}.");
                    break;
                }
                catch (Exception ex)
                {
                    currentAttempt++;
                    
                    if (currentAttempt > maxRetries)
                    {
                        log.Error($"[User:({connection.AuthCredentials.Id},{username})] Connection failed after {currentAttempt} attempts.", ex);
                        throw;
                    }
                    log.Warn($"[User:({connection.AuthCredentials.Id},{username})] Connection attempt {currentAttempt}/{maxRetries} failed. Retrying in 1s... Error: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }

            return connection;
        }
    }
}
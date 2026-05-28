using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Timer = System.Timers.Timer;
using HEAppE.Exceptions.Internal;

namespace HEAppE.ConnectionPool
{
    public class ConnectionPool : IConnectionPool
    {
        private class ConnectionSlot
        {
            public ConnectionInfo ConnectionInfo { get; set; }
            public int ReferenceCount = 0;
            public DateTime LastReleasedTime = DateTime.UtcNow;
            public readonly SemaphoreSlim SlotSemaphore = new SemaphoreSlim(1, 1);
        }

        private class SharedUserContext
        {
            public readonly ConnectionSlot[] Slots;
            public readonly SemaphoreSlim UserSemaphore;
            public readonly SemaphoreSlim ActiveSessionsSemaphore;
            private int _roundRobinCounter = 0;

            public SharedUserContext(int capacity, int maxSessionsPerConnection)
            {
                Slots = new ConnectionSlot[capacity];
                for (int i = 0; i < capacity; i++) Slots[i] = new ConnectionSlot();
                UserSemaphore = new SemaphoreSlim(capacity, capacity);
                // Total concurrent sessions allowed is capacity (MaxConnectionsPerUser) * maxSessionsPerConnection
                ActiveSessionsSemaphore = new SemaphoreSlim(capacity * maxSessionsPerConnection, capacity * maxSessionsPerConnection);
            }

            public ConnectionSlot GetNextSlot()
            {
                var idx = (Interlocked.Increment(ref _roundRobinCounter) & 0x7FFFFFFF) % Slots.Length;
                return Slots[idx];
            }
        }

        private readonly IPoolableAdapter _adapter;
        private readonly ILogger _logger;
        private readonly string _masterNodeName;
        private readonly int? _port;
        private readonly int _maxConnectionsPerUser;
        private readonly int _maxSessionsPerConnection;
        private readonly int _minSize;
        private readonly TimeSpan _maxUnusedDuration;
        private int _currentTotalPhysicalConnectionsCount;
        
        private readonly ConcurrentDictionary<long, SharedUserContext> _userContexts;
        private static readonly ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>> _vaultCache 
            = new ConcurrentDictionary<long, Task<ClusterProjectCredentialVaultPart>>();
        private readonly int _connectionRetryAttempts = 3;
        private readonly int _connectionTimeoutMs = 30000;
        
        /// <summary>
        /// Maximum time (ms) to wait for a connection slot before throwing.
        /// Prevents zombie threads when the pool is saturated under high load.
        /// Callers should catch <see cref="ConnectionPoolExhaustedException"/> and return 429.
        /// </summary>
        private readonly int _acquireTimeoutMs = 10000;
            
        private readonly Timer poolCleanTimer;

        public ConnectionPool(string masterNodeName, string remoteTimeZone, int minSize, int maxSize, int maxSessionsPerConnection, int cleaningInterval, int maxUnusedDuration, IPoolableAdapter adapter, int retryAttempts, int timeoutMs, int? port, ILogger logger, int acquireTimeoutMs = 10000)
        {
            _logger = logger;
            _masterNodeName = masterNodeName;
            _port = port;
            _minSize = minSize;
            _maxConnectionsPerUser = maxSize; 
            _maxSessionsPerConnection = maxSessionsPerConnection;
            _adapter = adapter;
            _userContexts = new ConcurrentDictionary<long, SharedUserContext>();
            _connectionRetryAttempts = retryAttempts;
            _connectionTimeoutMs = timeoutMs;
            _acquireTimeoutMs = acquireTimeoutMs;

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                _maxUnusedDuration = TimeSpan.FromSeconds(maxUnusedDuration);
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false;
                _logger.LogDebug($"ConnectionPool initialized. Cleaning interval: {cleaningInterval}s, Max unused: {maxUnusedDuration}s, AcquireTimeout: {acquireTimeoutMs}ms");
            }
        }

        public async Task<ConnectionInfo> GetConnectionForUserAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
        {
            return await GetConnectionForUserInternalAsync(credentials, cluster, sshCaToken, lexisToken);
        }

        private async Task<ConnectionInfo> GetConnectionForUserInternalAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
        {
            _logger.LogDebug($"[User:{credentials.Id}] Requesting connection.");
            var userContext = _userContexts.GetOrAdd(credentials.Id, id => {
                _logger.LogDebug($"[User:{id}] Creating new SharedUserContext with capacity {_maxConnectionsPerUser}");
                return new SharedUserContext(_maxConnectionsPerUser, _maxSessionsPerConnection);
            });

            // Restrict maximum concurrent active SSH commands globally per user connection pool.
            // Use a timeout so that saturated-pool callers fail fast instead of becoming zombie threads.
            if (!await userContext.ActiveSessionsSemaphore.WaitAsync(_acquireTimeoutMs))
            {
                throw new ConnectionPoolExhaustedException(
                    $"[User:{credentials.Id}] SSH connection pool saturated: could not acquire active-session permit within {_acquireTimeoutMs}ms. " +
                    $"Current pool: {_maxConnectionsPerUser} connections × {_maxSessionsPerConnection} sessions.");
            }
            try
            {
                // Fast path: reuse existing active connection by finding the one with minimum load
                int minRefSlotIndex = -1;
                int minRefCount = int.MaxValue;
                ConnectionSlot bestSlot = null;

                for (int i = 0; i < userContext.Slots.Length; i++)
                {
                    var s = userContext.Slots[i];
                    await s.SlotSemaphore.WaitAsync();
                    try
                    {
                        if (s.ConnectionInfo != null && _adapter.IsConnected(s.ConnectionInfo.Connection))
                        {
                            if (s.ReferenceCount < minRefCount)
                            {
                                minRefCount = s.ReferenceCount;
                                minRefSlotIndex = i;
                                bestSlot = s;
                            }
                        }
                    }
                    finally { s.SlotSemaphore.Release(); }
                }

                if (bestSlot != null)
                {
                    // Reuse connection if it is under the max sessions limit
                    if (minRefCount < _maxSessionsPerConnection)
                    {
                        await bestSlot.SlotSemaphore.WaitAsync();
                        try
                        {
                            // Double check it wasn't disconnected
                            if (bestSlot.ConnectionInfo != null && _adapter.IsConnected(bestSlot.ConnectionInfo.Connection))
                            {
                                bestSlot.ReferenceCount++;
                                bestSlot.ConnectionInfo.LastUsed = DateTime.UtcNow;
                                _logger.LogDebug($"[User:{credentials.Id}] Reusing existing connection from slot {minRefSlotIndex}. RefCount: {bestSlot.ReferenceCount}");
                                return bestSlot.ConnectionInfo;
                            }
                        }
                        finally { bestSlot.SlotSemaphore.Release(); }
                    }
                }

                // Pre-clean any dead/disconnected connections to free up UserSemaphore permits before waiting on it
                for (int i = 0; i < userContext.Slots.Length; i++)
                {
                    var s = userContext.Slots[i];
                    if (s.ConnectionInfo != null && !_adapter.IsConnected(s.ConnectionInfo.Connection))
                    {
                        await s.SlotSemaphore.WaitAsync();
                        try
                        {
                            // Double check under lock
                            if (s.ConnectionInfo != null && !_adapter.IsConnected(s.ConnectionInfo.Connection))
                            {
                                var oldConnection = s.ConnectionInfo;
                                s.ConnectionInfo = null;
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        await _adapter.DisconnectAsync(oldConnection.Connection);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning($"Error while disconnecting old connection for user {oldConnection.AuthCredentials.Id} during pre-cleanup", ex);
                                    }
                                    finally
                                    {
                                        if (oldConnection.Connection is IDisposable disposable)
                                        {
                                            try { disposable.Dispose(); } catch { /* ignore */ }
                                        }
                                    }
                                });
                                Interlocked.Decrement(ref _currentTotalPhysicalConnectionsCount);
                                userContext.UserSemaphore.Release();
                                _logger.LogDebug($"[User:{credentials.Id}] Pre-cleaned dead connection in slot {i}. Released UserSemaphore permit.");
                            }
                        }
                        finally { s.SlotSemaphore.Release(); }
                    }
                }

                _logger.LogDebug($"[User:{credentials.Id}] No idle connection found under session limit. Waiting for slot semaphore (Available: {userContext.UserSemaphore.CurrentCount})...");
                
                await EnsureVaultDataLoadedAsync(credentials);
                if (!await userContext.UserSemaphore.WaitAsync(_acquireTimeoutMs))
                {
                    throw new ConnectionPoolExhaustedException(
                        $"[User:{credentials.Id}] SSH connection pool saturated: could not acquire connection slot within {_acquireTimeoutMs}ms. " +
                        $"Pool limit: {_maxConnectionsPerUser} physical connections.");
                }

                try
                {
                    ConnectionSlot slot = null;
                    // Find an empty slot since we secured a permit to create one
                    for (int i = 0; i < userContext.Slots.Length; i++)
                    {
                        if (userContext.Slots[i].ConnectionInfo == null || !_adapter.IsConnected(userContext.Slots[i].ConnectionInfo.Connection))
                        {
                            slot = userContext.Slots[i];
                            break;
                        }
                    }

                    // Fallback (should theoretically not happen since permits == empty slots)
                    if (slot == null) slot = userContext.GetNextSlot();

                    await slot.SlotSemaphore.WaitAsync();
                    try
                    {
                        if (slot.ConnectionInfo != null && _adapter.IsConnected(slot.ConnectionInfo.Connection))
                        {
                            _logger.LogDebug($"[User:{credentials.Id}] Slot became available with active connection during wait.");
                            userContext.UserSemaphore.Release();
                            slot.ReferenceCount++;
                            return slot.ConnectionInfo;
                        }

                        if (slot.ConnectionInfo != null)
                        {
                            var oldConnection = slot.ConnectionInfo;
                            slot.ConnectionInfo = null;
                            _ = Task.Run(async () =>
                            {
                                try
                                {
                                    await _adapter.DisconnectAsync(oldConnection.Connection);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"Error while disconnecting old connection for user {oldConnection.AuthCredentials.Id} during replacement", ex);
                                }
                                finally
                                {
                                    if (oldConnection.Connection is IDisposable disposable)
                                    {
                                        try { disposable.Dispose(); } catch { /* ignore */ }
                                    }
                                }
                            });
                            Interlocked.Decrement(ref _currentTotalPhysicalConnectionsCount);
                            userContext.UserSemaphore.Release();
                        }

                        _logger.LogDebug($"[User:{credentials.Id}] Initializing new physical connection. Total connections: {_currentTotalPhysicalConnectionsCount + 1}");
                        var newConnection = await InitializeConnectionAsync(credentials, cluster, sshCaToken, lexisToken);
                        slot.ConnectionInfo = newConnection;
                        slot.ReferenceCount = 0;

                        Interlocked.Increment(ref _currentTotalPhysicalConnectionsCount);
                        if (poolCleanTimer != null && !poolCleanTimer.Enabled && _currentTotalPhysicalConnectionsCount > _minSize)
                        {
                            _logger.LogDebug("Starting cleanup timer.");
                            poolCleanTimer.Start();
                        }

                        slot.ReferenceCount++;
                        return slot.ConnectionInfo;
                    }
                    finally { slot.SlotSemaphore.Release(); }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"[User:{credentials.Id}] Connection setup failed", ex);
                    userContext.UserSemaphore.Release();
                    throw;
                }
            }
            catch (Exception ex)
            {
                // Release the active session permit if initialization failed
                userContext.ActiveSessionsSemaphore.Release();
                throw;
            }
        }

        public async Task ReturnConnectionAsync(ConnectionInfo connection)
        {
            if (connection == null) return;

            if (_userContexts.TryGetValue(connection.AuthCredentials.Id, out var userContext))
            {
                for (int i = 0; i < userContext.Slots.Length; i++)
                {
                    var slot = userContext.Slots[i];
                    await slot.SlotSemaphore.WaitAsync();
                    try
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
                            _logger.LogDebug($"[User:{connection.AuthCredentials.Id}] Connection returned to slot {i}. RefCount: {slot.ReferenceCount}");
                            
                            // Release the active session permit so another waiting command can run
                            userContext.ActiveSessionsSemaphore.Release();
                            return;
                        }
                    }
                    finally { slot.SlotSemaphore.Release(); }
                }
            }
            _logger.LogWarning($"[User:{connection.AuthCredentials.Id}] Attempted to return a connection that is not managed by this pool.");
        }

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                _logger.LogDebug($"Cleanup cycle started. Current physical connections: {_currentTotalPhysicalConnectionsCount}");
                int closedCount = 0;
                
                try
                {
                    foreach (var userEntry in _userContexts)
                    {
                        var userContext = userEntry.Value;
                        foreach (var slot in userContext.Slots)
                        {
                            ConnectionInfo connToRemove = null;
                            await slot.SlotSemaphore.WaitAsync();
                            try
                            {
                                if (slot.ConnectionInfo == null) continue;

                                bool isExpired = (DateTime.UtcNow - slot.LastReleasedTime) > _maxUnusedDuration;
                                _logger.LogDebug($"[User:{userEntry.Key}] Checking slot. RefCount: {slot.ReferenceCount}, LastReleased: {slot.LastReleasedTime}, IsExpired: {isExpired}, will expire in: {(slot.LastReleasedTime + _maxUnusedDuration) - DateTime.UtcNow}");
                                if (slot.ReferenceCount == 0 && isExpired && _currentTotalPhysicalConnectionsCount > _minSize)
                                {
                                    connToRemove = slot.ConnectionInfo;
                                    slot.ConnectionInfo = null;
                                }
                            }
                            finally { slot.SlotSemaphore.Release(); }

                            if (connToRemove != null)
                            {
                                _logger.LogDebug($"[User:{userEntry.Key}] Closing idle expired connection.");
                                await RemovePhysicalConnectionAsync(connToRemove, userContext);
                                closedCount++;
                            }
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError(ex, "Pool cleanup error"); }
                finally
                {
                    if (closedCount > 0) _logger.LogDebug($"Cleanup finished. Closed {closedCount} connections.");
                    if (poolCleanTimer != null && _currentTotalPhysicalConnectionsCount > _minSize)
                    {
                        poolCleanTimer.Start();
                    }
                    else
                    {
                        _logger.LogDebug("Cleanup timer stopped (pool at or below minSize).");
                    }
                }
            });
        }

        private async Task EnsureVaultDataLoadedAsync(ClusterAuthenticationCredentials cred)
        {
            if (cred.IsVaultDataLoaded) return;
            
            _logger.LogDebug($"[User:{cred.Id}] Loading vault data...");
            var vaultTask = _vaultCache.GetOrAdd(cred.Id, async id =>
            {
                _logger.LogDebug($"[User:{id}] Fetching vault data from service (Shared Task).");
                var connector = new VaultConnector(_logger);
                return await connector.GetClusterAuthenticationCredentials(id);
            });
            
            try 
            {
                var vaultData = await vaultTask;
                cred.ImportVaultData(vaultData);
            }
            catch (Exception ex)
            {
                _logger.LogError($"[User:{cred.Id}] Failed to load vault data", ex);
                _vaultCache.TryRemove(cred.Id, out _);
                throw;
            }
        }

        private async Task RemovePhysicalConnectionAsync(ConnectionInfo connection, SharedUserContext context)
        {
            try 
            { 
                await _adapter.DisconnectAsync(connection.Connection); 
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error while disconnecting connection for user {connection.AuthCredentials.Id}", ex);
            }
            finally
            {
                Interlocked.Decrement(ref _currentTotalPhysicalConnectionsCount);
                context.UserSemaphore.Release();
                _logger.LogDebug($"[User:{connection.AuthCredentials.Id}] Physical connection removed. Semaphore released.");
            }
        }
        
        private async Task<ConnectionInfo> InitializeConnectionAsync(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken, string lexisToken)
        {
            int maxRetries = _connectionRetryAttempts;
            int currentAttempt = 0;
            ConnectionInfo connection = new ConnectionInfo { LastUsed = DateTime.UtcNow, AuthCredentials = cred };
            
            // Note: username might change after CreateConnectionObjectAsync (e.g. for SSH CA)
            string username = cred.Username;

            while (true)
            {
                try
                {
                    // Always create a fresh connection object for each attempt to avoid stale socket states after transient failures
                    var connectionObject = await _adapter.CreateConnectionObjectAsync(_masterNodeName, cred, cluster, sshCaToken, lexisToken, cluster.Port ?? _port);
                    connection.Connection = connectionObject;

                    if (connectionObject is SshClient sshClient)
                    {
                        username = sshClient.ConnectionInfo.Username;
                        sshClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs);
                    }
                    else if (connectionObject is ScpClient scpClient)
                    {
                        username = scpClient.ConnectionInfo.Username;
                        scpClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs);
                    }
                    else if (connectionObject is SftpClient sftpClient)
                    {
                        username = sftpClient.ConnectionInfo.Username;
                        sftpClient.ConnectionInfo.Timeout = TimeSpan.FromMilliseconds(_connectionTimeoutMs);
                    }

                    if (currentAttempt == 0)
                    {
                        _logger.LogInformation($"[User:({cred.Id},{username})] Initializing connection. Max retries: {maxRetries}, Timeout: {_connectionTimeoutMs}ms");
                    }

                    await _adapter.ConnectAsync(connection.Connection);
                    _logger.LogInformation($"[User:({cred.Id},{username})] Connection initialized successfully on attempt {currentAttempt + 1}.");
                    break;
                }
                catch (Exception ex)
                {
                    // Clean up the failed connection object before retrying
                    if (connection.Connection is IDisposable disposable)
                    {
                        try { disposable.Dispose(); } catch { /* ignore */ }
                    }
                    connection.Connection = null;

                    currentAttempt++;
                    if (currentAttempt > maxRetries)
                    {
                        _logger.LogError(ex, $"[User:({cred.Id},{username})] Connection failed after {currentAttempt} attempts.");
                        throw;
                    }
                    _logger.LogWarning($"[User:({cred.Id},{username})] Connection attempt {currentAttempt}/{maxRetries + 1} failed. Retrying in 1s... Error: {ex.Message}");
                    await Task.Delay(1000);
                }
            }

            return connection;
        }
    }
}
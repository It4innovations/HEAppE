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
        
        private readonly ConcurrentDictionary<(long, long?), SharedUserContext> _userContexts;
        private readonly int _connectionRetryAttempts = 3;
        private readonly int _connectionTimeoutMs = 30000;
        private volatile bool _disposed;
        
        /// <summary>
        /// Maximum time (ms) to wait for a connection slot before throwing.
        /// Prevents zombie threads when the pool is saturated under high load.
        /// Callers should catch <see cref="ConnectionPoolExhaustedException"/> and return 429.
        /// </summary>
        private readonly int _acquireTimeoutMs = 10000;
        private readonly Timer poolCleanTimer;

        private string TargetNodeStr => _port.HasValue ? $"{_masterNodeName}:{_port}" : _masterNodeName;

        private string GetProtocolName(object? connectionObj = null)
        {
            if (connectionObj is SshClient) return "SSH";
            if (connectionObj is SftpClient) return "SFTP";
            if (connectionObj is ScpClient) return "SCP";
            if (connectionObj is HttpConnection) return "HTTP";

            var adapterName = _adapter?.GetType().Name ?? "";
            if (adapterName.Contains("Ssh", StringComparison.OrdinalIgnoreCase)) return "SSH";
            if (adapterName.Contains("Sftp", StringComparison.OrdinalIgnoreCase)) return "SFTP";
            if (adapterName.Contains("Http", StringComparison.OrdinalIgnoreCase)) return "HTTP";

            return string.IsNullOrEmpty(adapterName) ? "UNKNOWN" : adapterName.Replace("Connector", "").Replace("FileSystem", "");
        }

        public ConnectionPool(string masterNodeName, string remoteTimeZone, int minSize, int maxSize, int maxSessionsPerConnection, int cleaningInterval, int maxUnusedDuration, IPoolableAdapter adapter, int retryAttempts, int timeoutMs, int? port, ILogger logger, int acquireTimeoutMs = 10000)
        {
            _logger = logger;
            _masterNodeName = masterNodeName;
            _port = port;
            _minSize = minSize;
            _maxConnectionsPerUser = maxSize; 
            _maxSessionsPerConnection = maxSessionsPerConnection;
            _adapter = adapter;
            _userContexts = new ConcurrentDictionary<(long, long?), SharedUserContext>();
            _connectionRetryAttempts = retryAttempts;
            _connectionTimeoutMs = timeoutMs;
            _acquireTimeoutMs = acquireTimeoutMs;

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                _maxUnusedDuration = TimeSpan.FromSeconds(maxUnusedDuration);
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false;
                _logger.LogDebug($"ConnectionPool initialized for target {GetProtocolName()}://{TargetNodeStr}. Cleaning interval: {cleaningInterval}s, Max unused: {maxUnusedDuration}s, AcquireTimeout: {acquireTimeoutMs}ms");
            }
        }

        public async Task<ConnectionInfo> GetConnectionForUserAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken, Func<Task<string>>? refreshSshCaToken = null)
        {
            return await GetConnectionForUserInternalAsync(credentials, cluster, sshCaToken, lexisToken, refreshSshCaToken);
        }

        public async Task<ConnectionInfo> GetConnectionForUserAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken)
        {
            return await GetConnectionForUserAsync(credentials, cluster, sshCaToken, lexisToken, null);
        }

        private async Task<ConnectionInfo> GetConnectionForUserInternalAsync(ClusterAuthenticationCredentials credentials, Cluster cluster, string sshCaToken, string lexisToken, Func<Task<string>>? refreshSshCaToken = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionPool), "Cannot acquire connection from a disposed ConnectionPool.");

            _logger.LogDebug($"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Requesting connection.");
            var poolKey = (credentials.Id, credentials.SessionUserId);
            var userContext = _userContexts.GetOrAdd(poolKey, key => {
                _logger.LogDebug($"[User:{credentials.Username} (ID:{key.Item1})] [SessionUser:{key.Item2}] [Target:{GetProtocolName()}://{TargetNodeStr}] Creating new SharedUserContext with capacity {_maxConnectionsPerUser}");
                return new SharedUserContext(_maxConnectionsPerUser, _maxSessionsPerConnection);
            });

            // Restrict maximum concurrent active SSH commands globally per user connection pool.
            // Use a timeout so that saturated-pool callers fail fast instead of becoming zombie threads.
            if (!await userContext.ActiveSessionsSemaphore.WaitAsync(_acquireTimeoutMs))
            {
                throw new ConnectionPoolExhaustedException(
                    $"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Connection pool saturated: could not acquire active-session permit within {_acquireTimeoutMs}ms. " +
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
                                _logger.LogDebug($"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName(bestSlot.ConnectionInfo.Connection)}://{TargetNodeStr}] Reusing existing connection from slot {minRefSlotIndex}. RefCount: {bestSlot.ReferenceCount}");
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
                                        _logger.LogWarning($"Error while disconnecting old connection for user {oldConnection.AuthCredentials.Username} (ID:{oldConnection.AuthCredentials.Id}) [Target:{GetProtocolName(oldConnection.Connection)}://{TargetNodeStr}] during pre-cleanup", ex);
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
                                _logger.LogDebug($"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Pre-cleaned dead connection in slot {i}. Released UserSemaphore permit.");
                            }
                        }
                        finally { s.SlotSemaphore.Release(); }
                    }
                }

                _logger.LogDebug($"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] No idle connection found under session limit. Waiting for slot semaphore (Available: {userContext.UserSemaphore.CurrentCount})...");
                
                await EnsureVaultDataLoadedAsync(credentials);
                if (!await userContext.UserSemaphore.WaitAsync(_acquireTimeoutMs))
                {
                    throw new ConnectionPoolExhaustedException(
                        $"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Connection pool saturated: could not acquire connection slot within {_acquireTimeoutMs}ms. " +
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
                            _logger.LogDebug($"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName(slot.ConnectionInfo.Connection)}://{TargetNodeStr}] Slot became available with active connection during wait.");
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
                                    _logger.LogWarning($"Error while disconnecting old connection for user {oldConnection.AuthCredentials.Username} (ID:{oldConnection.AuthCredentials.Id}) [Target:{GetProtocolName(oldConnection.Connection)}://{TargetNodeStr}] during replacement", ex);
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

                        _logger.LogDebug($"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Initializing new physical connection. Total connections: {_currentTotalPhysicalConnectionsCount + 1}");
                        var newConnection = await InitializeConnectionAsync(credentials, cluster, sshCaToken, lexisToken, refreshSshCaToken);
                        slot.ConnectionInfo = newConnection;
                        slot.ReferenceCount = 0;

                        Interlocked.Increment(ref _currentTotalPhysicalConnectionsCount);
                        if (poolCleanTimer != null && !poolCleanTimer.Enabled && _currentTotalPhysicalConnectionsCount > _minSize)
                        {
                            _logger.LogDebug($"Starting cleanup timer for {GetProtocolName()}://{TargetNodeStr}.");
                            poolCleanTimer.Start();
                        }

                        slot.ReferenceCount++;
                        return slot.ConnectionInfo;
                    }
                    finally { slot.SlotSemaphore.Release(); }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[User:{credentials.Username} (ID:{credentials.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Connection setup failed");
                    userContext.UserSemaphore.Release();
                    throw;
                }
            }
            catch (Exception)
            {
                // Release the active session permit if initialization failed
                userContext.ActiveSessionsSemaphore.Release();
                throw;
            }
        }

        public async Task ReturnConnectionAsync(ConnectionInfo connection)
        {
            if (connection == null)
                return;

            var poolKey = (connection.AuthCredentials.Id, connection.AuthCredentials.SessionUserId);
            if (_userContexts.TryGetValue(poolKey, out var userContext))
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
                            _logger.LogDebug($"[User:{connection.AuthCredentials.Username} (ID:{connection.AuthCredentials.Id})] [Target:{GetProtocolName(connection.Connection)}://{TargetNodeStr}] Connection returned to slot {i}. RefCount: {slot.ReferenceCount}");
                            
                            // Release the active session permit so another waiting command can run
                            userContext.ActiveSessionsSemaphore.Release();
                            return;
                        }
                    }
                    finally { slot.SlotSemaphore.Release(); }
                }
            }
            _logger.LogWarning($"[User:{connection.AuthCredentials.Username} (ID:{connection.AuthCredentials.Id})] [Target:{GetProtocolName(connection.Connection)}://{TargetNodeStr}] Attempted to return a connection that is not managed by this pool.");
        }

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_disposed) return;

            _ = Task.Run(async () =>
            {
                if (_disposed) return;
                _logger.LogDebug($"Cleanup cycle started for target {GetProtocolName()}://{TargetNodeStr}. Current physical connections: {_currentTotalPhysicalConnectionsCount}");
                int closedCount = 0;
                
                try
                {
                    foreach (var userEntry in _userContexts)
                    {
                        if (_disposed) return;
                        var userContext = userEntry.Value;
                        foreach (var slot in userContext.Slots)
                        {
                            if (_disposed) return;
                            ConnectionInfo connToRemove = null;
                            await slot.SlotSemaphore.WaitAsync();
                            try
                            {
                                if (slot.ConnectionInfo == null) continue;

                                string username = slot.ConnectionInfo?.AuthCredentials?.Username ?? "N/A";
                                string protocol = GetProtocolName(slot.ConnectionInfo?.Connection);
                                bool isExpired = (DateTime.UtcNow - slot.LastReleasedTime) > _maxUnusedDuration;
                                bool hasActiveTunnels = slot.ConnectionInfo != null && _adapter.HasActiveForwardedPorts(slot.ConnectionInfo.Connection);
                                
                                if (hasActiveTunnels)
                                {
                                    // Connection hosts active SSH forwarded ports (data transfer tunnels). Keep it alive.
                                    slot.LastReleasedTime = DateTime.UtcNow;
                                    _logger.LogDebug($"[User:{username} (ID:{userEntry.Key.Item1})] [Target:{protocol}://{TargetNodeStr}] Slot has active SSH tunnels. Keeping connection alive and resetting LastReleasedTime.");
                                    continue;
                                }

                                _logger.LogDebug($"[User:{username} (ID:{userEntry.Key.Item1})] [Target:{protocol}://{TargetNodeStr}] Checking slot. RefCount: {slot.ReferenceCount}, LastReleased: {slot.LastReleasedTime}, IsExpired: {isExpired}, will expire in: {(slot.LastReleasedTime + _maxUnusedDuration) - DateTime.UtcNow}");
                                if (slot.ReferenceCount == 0 && isExpired && _currentTotalPhysicalConnectionsCount > _minSize)
                                {
                                    connToRemove = slot.ConnectionInfo;
                                    slot.ConnectionInfo = null;
                                }
                            }
                            finally { slot.SlotSemaphore.Release(); }

                            if (connToRemove != null)
                            {
                                string username = connToRemove.AuthCredentials?.Username ?? "N/A";
                                _logger.LogDebug($"[User:{username} (ID:{userEntry.Key.Item1})] [Target:{GetProtocolName(connToRemove.Connection)}://{TargetNodeStr}] Closing idle expired connection.");
                                await RemovePhysicalConnectionAsync(connToRemove, userContext);
                                closedCount++;
                            }
                        }

                        // Check if all slots are empty and idle, prune userContext from dictionary
                        bool allEmpty = true;
                        foreach (var slot in userContext.Slots)
                        {
                            if (slot.ConnectionInfo != null || slot.ReferenceCount > 0)
                            {
                                allEmpty = false;
                                break;
                            }
                        }

                        if (allEmpty)
                        {
                            int acquiredSlots = 0;
                            try
                            {
                                for (int i = 0; i < userContext.Slots.Length; i++)
                                {
                                    if (userContext.Slots[i].SlotSemaphore.Wait(0))
                                    {
                                        acquiredSlots++;
                                        if (userContext.Slots[i].ConnectionInfo != null || userContext.Slots[i].ReferenceCount > 0)
                                        {
                                            allEmpty = false;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        allEmpty = false;
                                        break;
                                    }
                                }

                                if (allEmpty)
                                {
                                    if (((System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<(long, long?), SharedUserContext>>)_userContexts).Remove(
                                        new System.Collections.Generic.KeyValuePair<(long, long?), SharedUserContext>(userEntry.Key, userContext)))
                                    {
                                        _logger.LogDebug($"[User:ID:{userEntry.Key.Item1}] [SessionUser:{userEntry.Key.Item2}] [Target:{GetProtocolName()}://{TargetNodeStr}] Removed idle empty SharedUserContext from pool.");
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < acquiredSlots; i++)
                                {
                                    userContext.Slots[i].SlotSemaphore.Release();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex) { _logger.LogError(ex, $"Pool cleanup error for target {GetProtocolName()}://{TargetNodeStr}"); }
                finally
                {
                    if (closedCount > 0) _logger.LogDebug($"Cleanup finished for {GetProtocolName()}://{TargetNodeStr}. Closed {closedCount} connections.");
                    if (!_disposed && poolCleanTimer != null && _currentTotalPhysicalConnectionsCount > _minSize)
                    {
                        poolCleanTimer.Start();
                    }
                    else
                    {
                        _logger.LogDebug($"Cleanup timer stopped for {GetProtocolName()}://{TargetNodeStr} (pool at or below minSize).");
                    }
                }
            });
        }

        private async Task EnsureVaultDataLoadedAsync(ClusterAuthenticationCredentials cred)
        {
            if (cred.IsVaultDataLoaded) return;
            
            _logger.LogDebug($"[User:{cred.Username} (ID:{cred.Id})] Loading vault data...");
            var connector = new VaultConnector(_logger);
            try 
            {
                var vaultData = await connector.GetClusterAuthenticationCredentials(cred.Id);
                cred.ImportVaultData(vaultData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[User:{cred.Username} (ID:{cred.Id})] Failed to load vault data");
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
                _logger.LogWarning(ex, $"Error while disconnecting connection for user {connection.AuthCredentials.Username} (ID:{connection.AuthCredentials.Id}) [Target:{GetProtocolName(connection.Connection)}://{TargetNodeStr}]");
            }
            finally
            {
                if (connection.Connection is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error while disposing connection for user {connection.AuthCredentials.Username} (ID:{connection.AuthCredentials.Id}) [Target:{GetProtocolName(connection.Connection)}://{TargetNodeStr}]");
                    }
                }
                Interlocked.Decrement(ref _currentTotalPhysicalConnectionsCount);
                context.UserSemaphore.Release();
                _logger.LogDebug($"[User:{connection.AuthCredentials.Username} (ID:{connection.AuthCredentials.Id})] [Target:{GetProtocolName(connection.Connection)}://{TargetNodeStr}] Physical connection removed. Semaphore released.");
            }
        }




        
        private static bool IsTokenExpired(string? token, int bufferSeconds = 5)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(token))
                {
                    var jwtToken = tokenHandler.ReadJwtToken(token);
                    if (jwtToken.ValidTo != DateTime.MinValue)
                    {
                        return jwtToken.ValidTo <= DateTime.UtcNow.AddSeconds(bufferSeconds);
                    }
                }
            }
            catch { /* Ignore non-JWT tokens */ }
            return false;
        }

        private async Task<ConnectionInfo> InitializeConnectionAsync(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken, string lexisToken, Func<Task<string>>? refreshSshCaToken = null)
        {
            await EnsureVaultDataLoadedAsync(cred);

            int maxRetries = _connectionRetryAttempts;
            int currentAttempt = 0;
            ConnectionInfo connection = new ConnectionInfo { LastUsed = DateTime.UtcNow, AuthCredentials = cred };
            
            // Note: username might change after CreateConnectionObjectAsync (e.g. for SSH CA)
            string username = cred.Username;
            Exception? firstException = null;
            string activeSshCaToken = sshCaToken;

            while (true)
            {
                try
                {
                    if (IsTokenExpired(activeSshCaToken, 5) && refreshSshCaToken != null)
                    {
                        _logger.LogInformation($"[User:{username} (ID:{cred.Id})] SSH CA token is expired or expiring soon. Refreshing via callback...");
                        var refreshed = await refreshSshCaToken();
                        if (!string.IsNullOrWhiteSpace(refreshed))
                        {
                            activeSshCaToken = refreshed;
                            _logger.LogInformation($"[User:{username} (ID:{cred.Id})] SSH CA token successfully refreshed before connection attempt.");
                        }
                    }

                    // Always create a fresh connection object for each attempt to avoid stale socket states after transient failures
                    var connectionObject = await _adapter.CreateConnectionObjectAsync(_masterNodeName, cred, cluster, activeSshCaToken, lexisToken, cluster.Port ?? _port);
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
                        _logger.LogInformation($"[User:{username} (ID:{cred.Id})] [Target:{GetProtocolName(connectionObject)}://{TargetNodeStr}] Initializing connection. Max retries: {maxRetries}, Timeout: {_connectionTimeoutMs}ms");
                    }

                    await _adapter.ConnectAsync(connection.Connection);
                    _logger.LogInformation($"[User:{username} (ID:{cred.Id})] [Target:{GetProtocolName(connectionObject)}://{TargetNodeStr}] Connection initialized successfully on attempt {currentAttempt + 1}.");
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

                    if (firstException == null)
                    {
                        firstException = ex;
                    }

                    if (ex is HEAppE.Exceptions.External.SshCAServiceTypeException sshCaEx)
                    {
                        _logger.LogWarning(sshCaEx, $"[User:{username} (ID:{cred.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] SSH CA error during connection setup.");
                        if (refreshSshCaToken != null && currentAttempt < maxRetries)
                        {
                            _logger.LogInformation($"[User:{username} (ID:{cred.Id})] Attempting to refresh SSH CA token after SSH CA error...");
                            var refreshed = await refreshSshCaToken();
                            if (!string.IsNullOrWhiteSpace(refreshed) && refreshed != activeSshCaToken)
                            {
                                activeSshCaToken = refreshed;
                                _logger.LogInformation($"[User:{username} (ID:{cred.Id})] SSH CA token refreshed. Retrying connection attempt {currentAttempt + 1}...");
                                currentAttempt++;
                                int refreshDelayMs = Math.Min(1000 * (int)Math.Pow(2, currentAttempt - 1), 8000);
                                await Task.Delay(refreshDelayMs);
                                continue;
                            }
                        }
                        throw firstException ?? ex;
                    }

                    currentAttempt++;
                    if (currentAttempt > maxRetries)
                    {
                        _logger.LogError(firstException ?? ex, $"[User:{username} (ID:{cred.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Connection failed after {currentAttempt} attempts.");
                        throw firstException ?? ex;
                    }
                    int delayMs = Math.Min(1000 * (int)Math.Pow(2, currentAttempt - 1), 8000);
                    _logger.LogWarning($"[User:{username} (ID:{cred.Id})] [Target:{GetProtocolName()}://{TargetNodeStr}] Connection attempt {currentAttempt}/{maxRetries + 1} failed. Retrying in {delayMs}ms... Error: {ex.Message}");
                    await Task.Delay(delayMs);
                }
            }

            return connection;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                if (poolCleanTimer != null)
                {
                    poolCleanTimer.Elapsed -= poolCleanTimer_Elapsed;
                    poolCleanTimer.Stop();
                    poolCleanTimer.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, $"[Target:{GetProtocolName()}://{TargetNodeStr}] Error disposing poolCleanTimer");
            }

            foreach (var userEntry in _userContexts)
            {
                var userContext = userEntry.Value;
                foreach (var slot in userContext.Slots)
                {
                    try
                    {
                        slot.SlotSemaphore?.Wait(100);
                        try
                        {
                            if (slot.ConnectionInfo?.Connection is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                            slot.ConnectionInfo = null;
                        }
                        finally
                        {
                            slot.SlotSemaphore?.Release();
                            slot.SlotSemaphore?.Dispose();
                        }
                    }
                    catch { /* ignore */ }
                }
                try { userContext.UserSemaphore?.Dispose(); } catch { }
                try { userContext.ActiveSessionsSemaphore?.Dispose(); } catch { }
            }
            _userContexts.Clear();
        }
    }
}
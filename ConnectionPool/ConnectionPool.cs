using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using HEAppE.DataAccessTier.Vault;
using HEAppE.DomainObjects.ClusterInformation;
using log4net;
using Timer = System.Timers.Timer;

namespace HEAppE.ConnectionPool;

public class ConnectionPool : IConnectionPool
{
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

        pool = new ConcurrentDictionary<long, ConcurrentStack<ConnectionInfo>>();
        _semaphore = new SemaphoreSlim(maxSize, maxSize);

        if (cleaningInterval > 0 && maxUnusedDuration > 0)
        {
            poolCleanTimer = new Timer(cleaningInterval * 1000);
            poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
            this.maxUnusedDuration = TimeSpan.FromSeconds(maxUnusedDuration);
        }
    }

    #endregion

    #region Timer Event Handlers

    private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
        log.Debug("Connection pool clean start.");

        foreach (var entry in pool)
        {
            var stack = entry.Value;
            var survivors = new ConcurrentStack<ConnectionInfo>();

            while (stack.TryPop(out var conn))
            {
                if (conn.LastUsed < DateTime.UtcNow - maxUnusedDuration &&
                    actualSize > minSize)
                {
                    adapter.Disconnect(conn.Connection);
                    Interlocked.Decrement(ref actualSize);
                }
                else
                {
                    survivors.Push(conn);
                }
            }

            pool[entry.Key] = survivors;
        }
    }

    #endregion

    #region Fields

    private readonly IPoolableAdapter adapter;
    private readonly ILog log;
    private readonly string _masterNodeName;
    private readonly int? _port;
    private readonly string _remoteTimeZone;
    private readonly int maxSize;
    private readonly TimeSpan maxUnusedDuration;
    private readonly int minSize;
    private readonly Timer poolCleanTimer;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentDictionary<long, ConcurrentStack<ConnectionInfo>> pool;
    private int actualSize;
    
    #endregion

    #region IConnectionPool Members

    public ConnectionInfo GetConnectionForUser(
        ClusterAuthenticationCredentials credentials,
        Cluster cluster,
        string sshCaToken)
    {
        var stack = pool.GetOrAdd(
            credentials.Id,
            _ => new ConcurrentStack<ConnectionInfo>());

        // 1️⃣ FAST PATH – existující connection pro stejný credential
        if (stack.TryPop(out var existing))
        {
            return existing;
        }

        // 2️⃣ SLOW PATH – musíme vytvořit nové, hlídáme globální limit
        _semaphore.Wait();

        try
        {
            // mezitím mohl jiný thread connection vrátit
            if (stack.TryPop(out existing))
            {
                _semaphore.Release();
                return existing;
            }

            var newConnection = InitializeConnection(credentials, cluster, sshCaToken);
            Interlocked.Increment(ref actualSize);

            if (poolCleanTimer != null && actualSize > minSize)
                poolCleanTimer.Start();

            return newConnection;
        }
        catch
        {
            _semaphore.Release();
            throw;
        }
    }


    public void ReturnConnection(ConnectionInfo connection)
    {
        connection.LastUsed = DateTime.UtcNow;

        var stack = pool.GetOrAdd(connection.AuthCredentials.Id, _ => new ConcurrentStack<ConnectionInfo>());

        // kontrola, aby nedošlo k duplicitnímu pushi stejného connection
        if (!stack.Contains(connection))
            stack.Push(connection);

        // nepouštíme _semaphore.Release(), protože connection je sdílená
    }




    #endregion

    #region Local Methods

    private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
    {
        if (!cred.IsVaultDataLoaded)
        {
            var _vaultConnector = new VaultConnector();
            var vaultData = _vaultConnector.GetClusterAuthenticationCredentials(cred.Id).GetAwaiter().GetResult();
            cred.ImportVaultData(vaultData);
        }

        var connectionObject =
            adapter.CreateConnectionObject(_masterNodeName, cred, cluster.ProxyConnection, sshCaToken, cluster.Port);
        var connection = new ConnectionInfo
        {
            Connection = connectionObject,
            LastUsed = DateTime.UtcNow,
            AuthCredentials = cred
        };
        adapter.Connect(connection.Connection);
        return connection;
    }

    private ConnectionInfo ExpandPoolAndGetConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
    {
        var connection = InitializeConnection(cred, cluster, sshCaToken);
        actualSize++;
        if (poolCleanTimer != null && actualSize > minSize)
            poolCleanTimer.Start();
        log.DebugFormat("Connection pool expanded with acc {0} - actual size {1}", cred.Username, actualSize);
        return connection;
    }

    private void RemoveConnection(ConnectionInfo connection)
    {
        // fyzické ukončení spojení – mimo jakoukoli synchronizaci
        adapter.Disconnect(connection.Connection);

        // atomicky snížíme velikost poolu
        Interlocked.Decrement(ref actualSize);

        log.DebugFormat(
            "Removed connection for {0} - actual size {1}",
            connection.AuthCredentials.Username,
            actualSize);
    }


    private ConnectionInfo FindOldestConnection()
    {
        ConnectionInfo oldestConnection = null;
        var oldestLastUsedTime = DateTime.UtcNow;
        foreach (var list in pool)
        foreach (var conn in list.Value)
            if (DateTime.Compare(oldestLastUsedTime, conn.LastUsed) >= 0)
            {
                oldestConnection = conn;
                oldestLastUsedTime = conn.LastUsed;
            }

        return oldestConnection;
    }

    private bool HasAnyFreeConnection()
    {
        var hasConnection = false;
        foreach (var item in pool)
            if (item.Value.Count > 0)
            {
                hasConnection = true;
                break;
            }

        return hasConnection;
    }

    private void AddConnectionToPool(ConnectionInfo connection)
    {
        var stack = pool.GetOrAdd(
            connection.AuthCredentials.Id,
            _ => new ConcurrentStack<ConnectionInfo>());

        stack.Push(connection);
    }


    #endregion
}
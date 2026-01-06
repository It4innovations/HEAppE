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
        
        // Konfigurace
        private readonly int minSize;
        private readonly int maxSize;
        private readonly TimeSpan maxUnusedDuration;
        
        // Stav
        private int actualSize;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<long, ConcurrentStack<ConnectionInfo>> pool;
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

            pool = new ConcurrentDictionary<long, ConcurrentStack<ConnectionInfo>>();
            
            // Semafor hlídá CELKOVÝ počet spojení v systému (aktivní + v poolu)
            _semaphore = new SemaphoreSlim(maxSize, maxSize);

            if (cleaningInterval > 0 && maxUnusedDuration > 0)
            {
                poolCleanTimer = new Timer(cleaningInterval * 1000);
                poolCleanTimer.Elapsed += poolCleanTimer_Elapsed;
                poolCleanTimer.AutoReset = false; // Prevence re-entry, pokud clean trvá déle než interval
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
            var stack = pool.GetOrAdd(
                credentials.Id,
                _ => new ConcurrentStack<ConnectionInfo>());

            //  FAST PATH – zkusíme vzít existující
            if (stack.TryPop(out var existing))
            {
                return existing;
            }

            //  SLOW PATH – musíme vytvořit nové, ale musíme respektovat maxSize
            // Zde čekáme, pokud je pool plný.
            _semaphore.Wait();

            try
            {
                // Double-check: Mezitím mohl někdo spojení vrátit, zatímco jsme čekali na semafor.
                // Pokud ano, vezmeme ho a OKAMŽITĚ vrátíme semafor (protože nevytváříme nové).
                if (stack.TryPop(out existing))
                {
                    _semaphore.Release();
                    return existing;
                }

                // Opravdu vytváříme nové spojení
                var newConnection = InitializeConnection(credentials, cluster, sshCaToken);
                
                Interlocked.Increment(ref actualSize);

                if (poolCleanTimer != null && !poolCleanTimer.Enabled && actualSize > minSize)
                {
                    poolCleanTimer.Start();
                }
                
                return newConnection;
            }
            catch
            {
                // Pokud nastane chyba při InitializeConnection, musíme vrátit token semaforu!
                _semaphore.Release();
                throw;
            }
        }

        public void ReturnConnection(ConnectionInfo connection)
        {
            if (connection == null) return;

            connection.LastUsed = DateTime.UtcNow;

            var stack = pool.GetOrAdd(connection.AuthCredentials.Id, _ => new ConcurrentStack<ConnectionInfo>());

            // OPTIMALIZACE: Odstraněno stack.Contains(connection).
            // Lineární prohledávání je při velkém provozu zabiják výkonu.
            stack.Push(connection);
            
            // Poznámka: Nevoláme _semaphore.Release(), protože spojení stále existuje (jen je v poolu).
        }

        #endregion

        #region Timer Event Handlers

        private void poolCleanTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                log.Debug("Connection pool clean start.");

                foreach (var entry in pool)
                {
                    var stack = entry.Value;
                    if (stack.IsEmpty) continue;

                    // Dočasný seznam pro spojení, která přežijí
                    var survivors = new List<ConnectionInfo>();
                    ConnectionInfo conn;

                    // Vytaháme všechna spojení ven ze stacku
                    while (stack.TryPop(out conn))
                    {
                        // Kontrola stáří a minimální velikosti poolu
                        if (conn.LastUsed < DateTime.UtcNow - maxUnusedDuration &&
                            actualSize > minSize)
                        {
                            // Spojení je staré -> smazat
                            RemoveConnection(conn);
                        }
                        else
                        {
                            // Spojení je v pořádku -> schovat
                            survivors.Add(conn);
                        }
                    }

                    // Vrátíme přeživší zpět do stacku
                    // (Iterujeme tak, aby pořadí ve stacku zůstalo rozumné, i když u poolu na tom tolik nezáleží)
                    for (int i = survivors.Count - 1; i >= 0; i--)
                    {
                        stack.Push(survivors[i]);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Error during connection pool cleanup", ex);
            }
            finally
            {
                // Restart timeru (pokud byl nastaven)
                if (poolCleanTimer != null && actualSize > minSize)
                {
                    poolCleanTimer.Start();
                }
            }
        }

        #endregion

        #region Local Methods

        private ConnectionInfo InitializeConnection(ClusterAuthenticationCredentials cred, Cluster cluster, string sshCaToken)
        {
            // Poznámka: Volání GetAwaiter().GetResult() je "sync-over-async" a může blokovat vlákna.
            // Pokud je to možné, přepis do async/await by byl lepší, ale to vyžaduje změnu interface.
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

        private void RemoveConnection(ConnectionInfo connection)
        {
            try
            {
                // Fyzické ukončení spojení
                adapter.Disconnect(connection.Connection);
            }
            catch (Exception ex)
            {
                log.Warn($"Error disconnecting connection for {connection.AuthCredentials.Username}", ex);
            }
            finally
            {
                // 1. Snížíme počítadlo
                Interlocked.Decrement(ref actualSize);

                // 2. Uvolníme semafor, aby se mohlo vytvořit nové spojení !!!
                _semaphore.Release();

                log.DebugFormat(
                    "Removed connection for {0} - actual size {1}",
                    connection.AuthCredentials.Username,
                    actualSize);
            }
        }

        #endregion
    }
}
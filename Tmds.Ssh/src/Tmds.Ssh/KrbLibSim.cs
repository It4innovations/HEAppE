/// Replaces Kerberos MIT library.
/// 
/// Requires:
/// - Kerberos krb5.conf file in path: CONFIG_FILE_PATH
///     - Kerberos user ticket caches:
///         - in path indicated by krb5.conf > [libdefaults] > default_ccache_name = DIR://...
///         - to have the filename TICKET_CACHE_PREFIX + [username]

using Kerberos.NET;
using Kerberos.NET.Client;
using Kerberos.NET.Entities;
using Kerberos.NET.Credentials;
using Kerberos.NET.Crypto;
using Kerberos.NET.Configuration;
using Kerberos.NET.Transport;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq;

namespace Tmds.Ssh;

//TODO: remove public keyword
public sealed class KrbLibSim
{
    #region private fields
    private const string CONFIG_FILE_PATH = "//etc/krb5.conf";
    private const string TICKET_CACHE_PREFIX = "tkt_";
    private const LogLevel MIN_LOG_LEVEL = LogLevel.Error;
    private static readonly Krb5Config s_krb5Conf;
    private static readonly string s_ticketCachePath;
    private static readonly ConcurrentDictionary<string, Krb5TicketCache> s_ticketCaches;
    private static ILoggerFactory s_loggerFactory;
    private static ILogger s_logger;
    private static int _ticketValidityBufferSeconds = 30; // invalidates ticket, on HasTicket(), if less than this seconds remain.
    #endregion

    /********************************/
    public const bool USE_MEMORY_CACHE = true; // true: the ticket cache is in memory; false: ticket cache is in file.
    public const bool ENABLED = true; // enable or disable this library.
    private const bool MEMORY_CACHE_ID_USERNAME_ONLY = true; // true: the memory cache id is the username; false: username and realm.
    /********************************/

    #region constructor
    static KrbLibSim()
    {
        // logs
        s_loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(MIN_LOG_LEVEL).AddConsole());
        s_logger = s_loggerFactory.CreateLogger("KrbLibSim");
        // inform that this library is active
        s_logger.LogInformation("|-> KrbLibSim is enabled <-|");

        s_krb5Conf = GetConfigurationFile();

        // expects the DefaultCCacheName property to be set in krb5.conf file
        s_ticketCachePath = s_krb5Conf.Defaults.DefaultCCacheName.Split(":")[1];

        //TODO: how to clear old caches? No need?
        //TODO: also, how to update caches? No need?
        if(USE_MEMORY_CACHE)
            s_ticketCaches = new ConcurrentDictionary<string, Krb5TicketCache>();
    }
    #endregion


    #region private methods
    /// <summary>
    /// Return the krb5.conf as a krb5Config instance.
    /// File must exist in the appropriate path.
    /// </summary>
    private static Krb5Config GetConfigurationFile()
    {
        if (!File.Exists(CONFIG_FILE_PATH))
        {
            throw new Exception($"ERROR: Kerberos config file does not exist at the location: {CONFIG_FILE_PATH}.");
        }

        // Kerberos configuration text
        string configValue = File.ReadAllText(CONFIG_FILE_PATH);
        // Parse text into a Kerberos configuration instance
        var krb5Config = Krb5Config.Parse(configValue);
        //TODO: Dns lookup not working with this library for linux.
        // Eventually requires Kerberos.NET.PortableDns (first version, 4.6.116) but at the moment, 
        // still doesn't work with our use case.
        krb5Config.Defaults.DnsLookupKdc = false;

        return krb5Config;
    }


    /// Gets the ticket cache path according to the specific user.
    /// </summary>
    private static string GetUserTicketCachePath(string username)
    {
        return Path.Join(s_ticketCachePath, TICKET_CACHE_PREFIX + username);
    }


    /// <summary>
    /// Get the id used to identify the ticket cache, given the ticket cache.
    /// </summary>
    private static string GetMemoryCacheId(Krb5TicketCache ticketCache)
    {
        return GetMemoryCacheId(ticketCache.Krb5Cache.DefaultPrincipalName.FullyQualifiedName,
                                ticketCache.DefaultDomain);
    }


    /// <summary>
    /// Get the id used to identify the ticket cache.
    /// </summary>
    private static string GetMemoryCacheId(string username, string defaultRealm)
    {
        if(MEMORY_CACHE_ID_USERNAME_ONLY)
            return username;
        else
            return username + "@" + defaultRealm;
    }

    #endregion


    #region public methods
    /// <summary>
    /// Create the Auth instance.
    /// </summary>
    public static Auth CreateAuth(string username, string service, bool useDelegatedCredentials)
    {
        return new Auth(s_krb5Conf, username, service, useDelegatedCredentials);
    }


    /// <summary>
    /// Add or update ticket cache from a specific user.
    /// </summary>
    public static string AddOrUpdateTicketCache(byte[] ticketCacheBytes)
    {
        var ticketCache = new Krb5TicketCache(ticketCacheBytes, s_loggerFactory);
        s_ticketCaches[GetMemoryCacheId(ticketCache)] = ticketCache;
        return ticketCache.Krb5Cache.DefaultPrincipalName.FullyQualifiedName; // username
    }

    /// <summary>
    /// Checks if the user has a valid ticket.
    /// </summary>
    public static bool HasTicket(string username, string address)
    {
        if(MEMORY_CACHE_ID_USERNAME_ONLY)
        {
            if(s_ticketCaches.TryGetValue(username, out Krb5TicketCache ticketCache))
            {
                var cacheEntry = ticketCache.GetCacheItem<KerberosClientCacheEntry>($"krbtgt/{s_krb5Conf.Defaults.DefaultRealm}");
                // removes _ticketValidityBufferSeconds from current time and returns true if the ticket hasn't expired.
                return cacheEntry.EndTime > DateTimeOffset.Now.AddSeconds(-_ticketValidityBufferSeconds);
            }
            else
                return false;
        }
        else
            throw new Exception("Error: Not implemented!");
    }
    #endregion


    #region Auth class
    public sealed class Auth : IDisposable
    {
        # region private fields
        private readonly KerberosClient _kerberosClient;
        private readonly string _username;
        private readonly string _service;
        private readonly string _defaultRealm;
        private readonly bool _useDelegatedCredentials;
        private readonly bool _saveTGSs;
        private KerberosKey? _micKey; /* d^_^b */
        private int _micCtxSeq;
        #endregion


        public bool IsSigned
        {
            get
            {
                //TODO: actually check if is signed, like NegotiateAuthentication.IsSigned;
                return _micKey != null;
            }
        }


        public Auth(Krb5Config krb5Config, string userPrincipalName, string targetService, bool useDelegatedCredentials, bool saveTGSs=false)
        {
            _username = userPrincipalName;
            _service = targetService;
            _useDelegatedCredentials = useDelegatedCredentials;
            _saveTGSs = saveTGSs;
            _defaultRealm = krb5Config.Defaults.DefaultRealm;

            // create client transports following protocol (1st UDP, 2nd TCP).
            var transports = new IKerberosTransport[]
            {
                new UdpKerberosTransport(s_loggerFactory),
                new TcpSimpleKerberosTransport(s_loggerFactory)
            };
            _kerberosClient = new KerberosClient(krb5Config, s_loggerFactory, transports);
            UpdateCache();
        }


        #region private methods
        /// <summary>
        /// Get the current service cache entry.
        /// </summary>
        private KerberosClientCacheEntry GetTGSEntry()
        {
            return _kerberosClient.Cache.GetCacheItem<KerberosClientCacheEntry>(_service);
        }


        /// <summary>
        /// Get the client's main TGT cache entry.
        /// </summary>
        private KerberosClientCacheEntry GetMainTGTEntry()
        {
            return _kerberosClient.Cache.GetCacheItem<KerberosClientCacheEntry>($"krbtgt/{_kerberosClient.DefaultDomain}");
        }


        /// <summary>
        /// Convert KerberosClientCacheEntry to TicketCacheEntry
        /// </summary>
        private static TicketCacheEntry ClientCacheEntryToTicketCacheEntry(KerberosClientCacheEntry kerberosClientCacheEntry)
        {
            kerberosClientCacheEntry.SessionKey.Usage = KeyUsage.EncTgsRepPartSessionKey;

            return new TicketCacheEntry
            {
                Key = kerberosClientCacheEntry.KdcResponse.Ticket.SName.FullyQualifiedName,
                Expires = kerberosClientCacheEntry.EndTime,
                RenewUntil = kerberosClientCacheEntry.RenewTill,
                Value = kerberosClientCacheEntry
            };
        }


        /// <summary>
        /// Get the memory cache id of this user.
        /// </summary>
        private string GetMyMemoryCacheId()
        {
            return GetMemoryCacheId(_username, _defaultRealm);
        }
        

        /// <summary>
        /// Get the ticket cache entries from the file for this user.
        /// </summary>
        private IEnumerable<TicketCacheEntry> GetTicketCacheEntries()
        {
            Krb5TicketCache userTicketCache;

            if (USE_MEMORY_CACHE)
            {
                if (!s_ticketCaches.TryGetValue(GetMyMemoryCacheId(), out userTicketCache))
                {
                    //TODO: what to do when it fails? Throw  (should be specific) or try file cache (probably not)?
                    throw new Exception("Error: Could not get the Krb5TicketCache from memory.");
                }
            }
            else
            {
                // load the ticket cache from a defined location
                userTicketCache = new Krb5TicketCache(GetUserTicketCachePath(_username), s_loggerFactory);
            }
            
            return userTicketCache.GetAll().ToList().Select(e => ClientCacheEntryToTicketCacheEntry((KerberosClientCacheEntry)e));
        }


        /// <summary>
        /// Updates the ticket cache and credential.
        /// </summary>
        private void UpdateCache()
        {
            //TODO: remove watch
            var watch = Stopwatch.StartNew();

            // load all tickets
            foreach (TicketCacheEntry entry in GetTicketCacheEntries())
            {
                _kerberosClient.Cache.Add(entry);
            }

            watch.Stop();
            Console.WriteLine($" ----------> UpdateCache: {watch.ElapsedMilliseconds}ms)");
        }


        /// <summary>
        /// Create the GSSAPI ApReq message to send to the server, for which we must have a TGS.
        /// </summary>
        private byte[] CreateGssApiApReqMessage()
        {
            var clientTGSEntry = GetTGSEntry();
            if (clientTGSEntry.KdcResponse == null)
            {
                s_logger.LogError($"User [{_username}] has no TGS for the service [{_service}].");
                throw new Exception("Unable to find TGS ticket.");
            }

            var rst = new RequestServiceTicket
            {
                ApOptions = ApOptions.MutualRequired,
                GssContextFlags = GssContextEstablishmentFlag.GSS_C_MUTUAL_FLAG |
                                    GssContextEstablishmentFlag.GSS_C_CONF_FLAG |
                                    GssContextEstablishmentFlag.GSS_C_INTEG_FLAG |
                                    GssContextEstablishmentFlag.GSS_C_TRANS_FLAG
            };

            if (_useDelegatedCredentials)
            {
                // get main credential
                var tgtEntry = GetMainTGTEntry();
                // wrap credential
                //TODO: MIT Kerberos seems to generate a bigger packages.
                // Are we doing enough security wise?
                var krbCred = KrbCred.WrapTicket(
                    tgtEntry.KdcResponse.Ticket,
                    new KrbCredInfo
                    {
                        Key = tgtEntry.SessionKey,
                        AuthTime = tgtEntry.AuthTime,
                        EndTime = tgtEntry.EndTime,
                        Flags = tgtEntry.Flags,
                        PName = tgtEntry.KdcResponse.CName,
                        Realm = tgtEntry.KdcResponse.CRealm,
                        RenewTill = tgtEntry.RenewTill,
                        SName = tgtEntry.KdcResponse.Ticket.SName,
                        SRealm = tgtEntry.KdcResponse.Ticket.Realm,
                        StartTime = tgtEntry.StartTime
                    }
                );

                var delegationInfo = new DelegationInfo(rst) { DelegationTicket = krbCred, DelegationOption = 1 };
                var checksum = KrbChecksum.EncodeDelegationChecksum(delegationInfo);
                rst.AuthenticatorChecksum = checksum;
            }
            
            // create ApReq
            var apReq = KrbApReq.CreateApReq(
                        clientTGSEntry.KdcResponse,
                        clientTGSEntry.SessionKey.AsKey(),
                        rst,
                        out KrbAuthenticator authenticator
                    );

            return GssApiToken.Encode(new(MechType.KerberosGssApi), apReq).ToArray();
        }


        /// <summary>
        /// Save the TGS ticket in the file ticket cache.
        /// </summary>
        private void SaveTGSOnFileCache()
        {
            var clientTGSEntry = GetTGSEntry();
            var ticketCacheEntry = ClientCacheEntryToTicketCacheEntry(clientTGSEntry);

            // load ticket cache file
            var userTicketCache = new Krb5TicketCache(GetUserTicketCachePath(_username), s_loggerFactory);
            // save entry in the file
            userTicketCache.Add(ticketCacheEntry);
        }
        #endregion


        #region public methods
        /// <summary>
        /// The kerberos client will try to get a TGS for the service service name.
        /// </summary>
        public async Task<byte[]> InitiateAuthentication()
        {
            var clientTGSEntry = GetTGSEntry();
            if (!clientTGSEntry.IsValid())
            {
                await _kerberosClient.GetServiceTicket(_service);
                if(_saveTGSs)
                    SaveTGSOnFileCache();
            }

            return CreateGssApiApReqMessage();
        }


        /// <summary>
        /// Decode and keep the GSSAPI token that is received from the server.
        /// </summary>
        public void DecodeAndDecryptGssApiToken(byte[] gssApiToken)
        {
            KerberosClientCacheEntry clientTGSEntry = GetTGSEntry();
            try
            {
                var srv_gssapi_token = GssApiToken.Decode(new ReadOnlyMemory<byte>(gssApiToken));
                var krbApRep = KrbApRep.DecodeApplication(srv_gssapi_token.Token);
                var decryptKrbApRep = new DecryptedKrbApRep(krbApRep);
                //TODO: check if i can obtain spn
                decryptKrbApRep.Decrypt(clientTGSEntry.SessionKey.AsKey());

                // save subsession key for MIC message
                _micKey = decryptKrbApRep.Response.SubSessionKey.AsKey(); /* d^_^b */
                // save sequence number for MIC message
                _micCtxSeq = decryptKrbApRep.Response.SequenceNumber.Value;
            }
            catch (Exception ex)
            {
                s_logger.LogError($"User [{_username}], service [{_service}] - Error in while trying to decode GSSAPI token received from the server: ", ex);
            }
        }


        /// <summary>
        /// Decode Kerberos GSSAPI token. DEBUG ONLY.
        /// </summary>
        public DelegationInfo DEBUG_DecodeAndDecryptGssApiToken(byte[] gssApiToken)
        {
            KerberosClientCacheEntry clientTGSEntry = GetTGSEntry();
            try
            {
                var srv_gssapi_token = GssApiToken.Decode(new ReadOnlyMemory<byte>(gssApiToken));
                var krbApReq = KrbApReq.DecodeApplication(srv_gssapi_token.Token);

                Func<ReadOnlyMemory<byte>, KrbAuthenticator> decoder = b => KrbAuthenticator.DecodeApplication(b);
                KrbAuthenticator decryptedAuthenticator = krbApReq.Authenticator.Decrypt(clientTGSEntry.SessionKey.AsKey(), KeyUsage.ApReqAuthenticator, decoder);
                DelegationInfo delegationInfo = decryptedAuthenticator.Checksum.DecodeDelegation();

                return delegationInfo;
            }
            catch (Exception ex)
            {
                s_logger.LogError($"User [{_username}], service [{_service}] - Error in while trying to decode GSSAPI token received from the server: ", ex);
            }
            return null;
        }


        /// <summary>
        /// Replaces Kerberos MIT GET_MIC method. <br/>
        /// Specified in: https://datatracker.ietf.org/doc/html/rfc4121#section-4.2.6.1<br/>
        /// Implemented in: krb5-1.20.1/src/lib/gssapi/krb5/k5sealv3.c --> gss_krb5int_make_seal_token_v3()<br/>
        /// </summary>
        public byte[] Get_MIC(byte[] message)
        {
            // subsession key
            KerberosKey key = _micKey; /* d^_^b */
            // sequence number
            int ctx__seq_send = _micCtxSeq;

            int tok_id = 0x0404; // KG2_TOK_MIC_MSG
            KeyUsage key_usage = KeyUsage.SamChecksum; // KRB5_KEYUSAGE_PA_SAM_CHALLENGE_CKSUM
            int cksumsize = 12; // ctx_cksumtype
            int bufsize = 16 + cksumsize;
            byte[] outbuf = new byte[bufsize];
            byte[] plain_data = new byte[message.Length + 16];

            // big-endian format
            /* TOK_ID */
            outbuf[0] = (byte)(tok_id >> 8);
            outbuf[1] = (byte)tok_id;
            /* flags */
            outbuf[2] = 0x04; // FLAG_ACCEPTOR_SUBKEY
            /* filler */
            outbuf[3] = 0xff;
            outbuf[4] = 0xff;
            outbuf[5] = 0xff;
            outbuf[6] = 0xff;
            outbuf[7] = 0xff;
            // ctx->seq_send
            for (int i = 0; i < 4; i++)
                outbuf[15 - i] = (byte)(ctx__seq_send >> i * 8);

            for (int i = 0; i < message.Length; i++)
                plain_data[i] = message[i];

            for (int i = 0; i < 16; i++)
                plain_data[i + message.Length] = outbuf[i];

            // calculate checksum
            //TODO: WARNING -> check if just doing this is enough, security wise.
            var cksum_struct = KrbChecksum.Create(plain_data, key, key_usage, ChecksumType.HMAC_SHA1_96_AES256);
            if (cksum_struct.Checksum.Length != cksumsize)
                return null;

            // copy checksum to the output
            byte[] cksum_data = cksum_struct.Checksum.ToArray();
            for (int i = 0; i < cksumsize; i++)
                outbuf[i + 16] = cksum_data[i];

            return outbuf;
        }


        /// <summary>
        /// Dispose of the KerberosClient.
        /// </summary>
        public void Dispose()
        {
            _kerberosClient.Dispose();
        }
        #endregion
    }
    #endregion
}
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using HEAppE.DataAccessTier;
using HEAppE.DataAccessTier.Vault.Settings;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApi.Configuration;

namespace HEAppE.RestApi;

public class HEAppEHealth
{
    public static HealthExt.HealthComponent_.Vault_.VaultInfo_ ConstructExtVaultInfo_(dynamic vaultInfo)
    {
        HealthExt.HealthComponent_.Vault_.VaultInfo_ result = null;
        if (vaultInfo != null)
            try {
                result = new()
                {
                    Initialized = vaultInfo.initialized,
                    Sealed = vaultInfo.@sealed,
                    StandBy = vaultInfo.standby,
                    PerformanceStandby = vaultInfo.performance_standby
                };
            } catch {
                result = null;
            }
        return result;
    }

    public static HealthExt ConstructHealthExt(bool isHealthy, bool databaseIsHealthy, bool vaultIsHealthy, DateTime timestamp, HealthExt.HealthComponent_.Vault_.VaultInfo_ vaultInfo)
    {
        return new HealthExt
        {
            IsHealthy = isHealthy,
            Timestamp = timestamp,
            Version = DeploymentInformationsConfiguration.Version,

            Component = new HealthExt.HealthComponent_ {
                Database = new HealthExt.HealthComponent_.Database_ {
                    IsHealthy = databaseIsHealthy
                },
                Vault = new HealthExt.HealthComponent_.Vault_ {
                    IsHealthy = vaultIsHealthy,
                    Info = vaultInfo
                }
            }
        };
    }

    public static DateTime GetCurrentTimestamp()
    {
        return DateTime.SpecifyKind(new SqlDateTime(DateTime.UtcNow).Value, DateTimeKind.Utc);
    }

    public static async Task<HealthExt> GetHealth(ILog log)
    {
        bool isHealthy = false, databaseIsHealthy = false, vaultIsHealthy = false;
        dynamic vaultInfo = null;
        int? timeoutMs = 1000; // let it be constant for now...
        var cancellationToken = new CancellationTokenSource(timeoutMs.Value).Token;
        var taskDatabaseCanConnect = SqlServerHealthCheck.DatabaseCanConnectAsync(log, MiddlewareContextSettings.ConnectionString, cancellationToken);
        var taskGetVaultHealth = VaultHealthCheck.GetVaultHealth(log, VaultConnectorSettings.VaultBaseAddress, timeoutMs.Value);
        await Task.WhenAll(taskDatabaseCanConnect, taskGetVaultHealth);

        if (taskDatabaseCanConnect.IsCompletedSuccessfully && taskDatabaseCanConnect.Result)
            databaseIsHealthy = true;

        if (taskGetVaultHealth.IsCompletedSuccessfully)
        {
            vaultInfo = taskGetVaultHealth.Result;
            if (vaultInfo != null && vaultInfo.initialized == true && vaultInfo.@sealed == false && vaultInfo.standby == false && vaultInfo.performance_standby == false)
                vaultIsHealthy = true;
        }

        if (databaseIsHealthy && vaultIsHealthy)
            isHealthy = true;

        return ConstructHealthExt(
            isHealthy,
            databaseIsHealthy,
            vaultIsHealthy,
            GetCurrentTimestamp(),
            ConstructExtVaultInfo_(vaultInfo)
        );
    }

    public static HealthExt DoProcessHealthReport(HealthReport healthReport)
    {
        var sqlEntry = healthReport.Entries["sql"];
        var sqlCanConnect = sqlEntry.Data.ContainsKey("canConnect") ? (bool)sqlEntry.Data["canConnect"] : false;
        bool databaseIsHealthy = sqlEntry.Status == HealthStatus.Healthy;

        var vaultEntry = healthReport.Entries["vault"];

        var vaultInfo = vaultEntry.Data.ContainsKey("vaultInfo") ? (HealthExt.HealthComponent_.Vault_.VaultInfo_)vaultEntry.Data["vaultInfo"] : null;
        bool vaultIsHealthy = vaultEntry.Status == HealthStatus.Healthy;

        bool isHealthy = databaseIsHealthy && vaultIsHealthy;

        DateTime timestamp = DateTime.MinValue;
        if (sqlEntry.Data.ContainsKey("timestamp"))
            if ((DateTime)sqlEntry.Data["timestamp"] > timestamp)
                timestamp = (DateTime)sqlEntry.Data["timestamp"];

        if (vaultEntry.Data.ContainsKey("timestamp"))
            if ((DateTime)vaultEntry.Data["timestamp"] > timestamp)
                timestamp = (DateTime)vaultEntry.Data["timestamp"];

        if (timestamp == DateTime.MinValue)
            timestamp = GetCurrentTimestamp();

        var healthExt = ConstructHealthExt(isHealthy, databaseIsHealthy, vaultIsHealthy,
            timestamp,
            vaultInfo
        );
        return healthExt;
    }

    public static Task ResponseWriter(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json";

        HealthExt healthExt = DoProcessHealthReport(healthReport);
        var result = JsonConvert.SerializeObject(healthExt);

        return context.Response.WriteAsync(result);
    }
}

public class SqlServerHealthCheck(IMemoryCache cacheProvider = null) : IHealthCheck
{
    IMemoryCache _cacheProvider = cacheProvider;
    const string _cacheKey = "HealthCheck/SQL";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HealthCheckResult result;
        IReadOnlyDictionary<string, object>? cacheEntry = null;
        try {
            if (_cacheProvider == null || !_cacheProvider.TryGetValue(_cacheKey, out cacheEntry))
            {
                cacheEntry = new Dictionary<string, object> {
                    {"canConnect", await DatabaseCanConnectAsync(LogManager.GetLogger(GetType()), MiddlewareContextSettings.ConnectionString, new CancellationTokenSource(1000).Token) },
                    {"timestamp", HEAppEHealth.GetCurrentTimestamp() }
                };
                _cacheProvider?.Set(_cacheKey, cacheEntry, TimeSpan.FromMilliseconds(HealthCheckSettings.HealthChecksCacheExpirationMs));
            }
            result = (bool)cacheEntry["canConnect"] ? HealthCheckResult.Healthy(null, cacheEntry) : HealthCheckResult.Unhealthy(null, null, cacheEntry);
        } catch (Exception e) {
            result = HealthCheckResult.Unhealthy(null, e, cacheEntry);
        }
        return result;
    }

    public static async Task<bool> DatabaseCanConnectAsync(ILog log, string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                // one second timeout is minimum possible (int)...
                ConnectTimeout = 1,
                CommandTimeout = 1
            };

            using (var connection = new SqlConnection(builder.ConnectionString))
            {
                var command = new SqlCommand("SELECT 1", connection);
                await connection.OpenAsync(cancellationToken);
                try
                {
                    _ = await command.ExecuteScalarAsync(cancellationToken);
                }
                finally
                {
                    connection.Close();
                }
            }
        }
        catch (Exception e)
        {
            log?.Error($"Database connection health check failed. Exception {e}");
            return false;
        }
        return true;
    }
}

public class VaultHealthCheck(IMemoryCache cacheProvider = null) : IHealthCheck
{
    IMemoryCache _cacheProvider = cacheProvider;
    const string _cacheKey = "HealthCheck/Vault";

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        HealthCheckResult result;
        IReadOnlyDictionary<string, object>? _cacheEntry = null;
        try {
            dynamic vaultInfo;

            if (_cacheProvider == null || !_cacheProvider.TryGetValue(_cacheKey, out _cacheEntry))
            {
                vaultInfo = await GetVaultHealth(LogManager.GetLogger(GetType()), VaultConnectorSettings.VaultBaseAddress, 1000);
                if (vaultInfo != null)
                    vaultInfo = HEAppEHealth.ConstructExtVaultInfo_(vaultInfo);
                _cacheEntry = new Dictionary<string, object>() {
                    { "vaultInfo",  vaultInfo},
                    { "timestamp", HEAppEHealth.GetCurrentTimestamp() }
                };
                _cacheProvider?.Set(_cacheKey, _cacheEntry, TimeSpan.FromMilliseconds(HealthCheckSettings.HealthChecksCacheExpirationMs));
            } else {
                vaultInfo = _cacheEntry["vaultInfo"];
            }

            if (vaultInfo != null && vaultInfo.Initialized == true && vaultInfo.@Sealed == false && vaultInfo.StandBy == false && vaultInfo.PerformanceStandby == false)
                result = HealthCheckResult.Healthy(null, _cacheEntry);
            else
                result = HealthCheckResult.Unhealthy(null, null, _cacheEntry);

        } catch (Exception e) {
            result = HealthCheckResult.Unhealthy(null, e, _cacheEntry);
        }
        return result;
    }
    public static async Task<object> GetVaultHealth(ILog log, string vaultBaseAddress, int timeoutMs)
    {
        using var httpClient = new HttpClient
        {
            BaseAddress = new Uri(vaultBaseAddress),
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };
        var path = $"/v1/sys/health/";
    
        try
        {
            var result = await httpClient.GetStringAsync(path);
            var response = JsonConvert.DeserializeObject<ExpandoObject>(result, new ExpandoObjectConverter());
            log?.Warn($"Obtained health information");
            return response;
        }
        catch (Exception e)
        {
            log?.Error($"Vault health check failed. Exception {e}");
        }

        return null;
    }
}

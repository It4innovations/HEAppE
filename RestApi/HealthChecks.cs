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
    public static HealthExt.HealthComponent_.Vault_.VaultInfo_ ConstructExtVaultHealth_(dynamic vaultHealth)
    {
        HealthExt.HealthComponent_.Vault_.VaultInfo_ result = null;
        if (vaultHealth != null)
            try {
                result = new()
                {
                    Initialized = vaultHealth.initialized,
                    Sealed = vaultHealth.@sealed,
                    StandBy = vaultHealth.standby,
                    PerformanceStandby = vaultHealth.performance_standby
                };
            } catch {
                result = null;
            }
        return result;
    }

    public static HealthExt ConstructHealthExt(bool isHealthy, bool databaseIsHealthy, bool vaultIsHealthy, HealthExt.HealthComponent_.Vault_.VaultInfo_ vaultInfo)
    {
        return new HealthExt
        {
            IsHealthy = isHealthy,
            Timestamp = DateTime.SpecifyKind(new SqlDateTime(DateTime.UtcNow).Value, DateTimeKind.Utc),
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

    public static async Task<HealthExt> GetHealth(ILog log)
    {
        bool isHealthy = false, databaseIsHealthy = false, vaultIsHealthy = false;
        dynamic vaultHealth = null;
        int? timeoutMs = 1000; // let it be constant for now...
        var cancellationToken = new CancellationTokenSource(timeoutMs.Value).Token;
        var taskDatabaseCanConnect = SqlServerHealthCheck.DatabaseCanConnectAsync(log, MiddlewareContextSettings.ConnectionString, cancellationToken);
        var taskGetVaultHealth = VaultHealthCheck.GetVaultHealth(log, VaultConnectorSettings.VaultBaseAddress, timeoutMs.Value);
        await Task.WhenAll(taskDatabaseCanConnect, taskGetVaultHealth);

        if (taskDatabaseCanConnect.IsCompletedSuccessfully && taskDatabaseCanConnect.Result)
            databaseIsHealthy = true;

        if (taskGetVaultHealth.IsCompletedSuccessfully)
        {
            vaultHealth = taskGetVaultHealth.Result;
            if (vaultHealth != null && vaultHealth.initialized == true && vaultHealth.@sealed == false && vaultHealth.standby == false && vaultHealth.performance_standby == false)
                vaultIsHealthy = true;
        }

        if (databaseIsHealthy && vaultIsHealthy)
            isHealthy = true;

        return ConstructHealthExt(
            isHealthy,
            databaseIsHealthy,
            vaultIsHealthy, ConstructExtVaultHealth_(vaultHealth)
        );
    }

    public static Task ResponseWriter(HttpContext context, HealthReport healthReport) {
        context.Response.ContentType = "application/json";

        var sqlEntry = healthReport.Entries["sql"];
        var sqlCanConnect = sqlEntry.Data.ContainsKey("canConnect") ? (bool)sqlEntry.Data["canConnect"] : false;
        bool databaseIsHealthy = sqlEntry.Status == HealthStatus.Healthy;

        var vaultEntry = healthReport.Entries["vault"];
        
        var vaultHealth = vaultEntry.Data.ContainsKey("vaultHealth") ? (HealthExt.HealthComponent_.Vault_.VaultInfo_)vaultEntry.Data["vaultHealth"] : null;
        bool vaultIsHealthy = vaultEntry.Status == HealthStatus.Healthy;

        bool isHealthy = databaseIsHealthy && vaultIsHealthy;

        var healthExt = ConstructHealthExt(isHealthy, databaseIsHealthy, vaultIsHealthy, vaultHealth);
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
        IReadOnlyDictionary<string, object>? data = null;
        try {
            bool canConnect;
            if (_cacheProvider == null || !_cacheProvider.TryGetValue(_cacheKey, out canConnect))
            {
                canConnect = await DatabaseCanConnectAsync(LogManager.GetLogger(GetType()), MiddlewareContextSettings.ConnectionString, new CancellationTokenSource(1000).Token);
                _cacheProvider?.Set(_cacheKey, canConnect, TimeSpan.FromMilliseconds(HealthCheckSettings.HealthChecksCacheExpirationMs));
            }
            data = new Dictionary<string, object>() {
                { "canConnect", canConnect }
            };
            result = canConnect ? HealthCheckResult.Healthy(null, data) : HealthCheckResult.Unhealthy(null, null, data);
        } catch (Exception e) {
            result = HealthCheckResult.Unhealthy(null, e, data);
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
        IReadOnlyDictionary<string, object>? data = null;
        try {
            dynamic vaultHealth;

            if (_cacheProvider == null || !_cacheProvider.TryGetValue(_cacheKey, out vaultHealth))
            {
                vaultHealth = await GetVaultHealth(LogManager.GetLogger(GetType()), VaultConnectorSettings.VaultBaseAddress, 1000);
                _cacheProvider?.Set(_cacheKey, (vaultHealth as ExpandoObject), TimeSpan.FromMilliseconds(HealthCheckSettings.HealthChecksCacheExpirationMs));
            }
            if (vaultHealth != null)
            {
                data = new Dictionary<string, object>() {
                    { "vaultHealth", HEAppEHealth.ConstructExtVaultHealth_(vaultHealth) }
                };
            }

            if (vaultHealth != null && vaultHealth.initialized == true && vaultHealth.@sealed == false && vaultHealth.standby == false && vaultHealth.performance_standby == false)
                result = HealthCheckResult.Healthy(null, data);
            else
                result = HealthCheckResult.Unhealthy(null, null, data);

        } catch (Exception e) {
            result = HealthCheckResult.Unhealthy(null, e, data);
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
            //var response = JsonSerializer.Deserialize<Dictionary<string, dynamic>>(result);
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

// https://blog.elmah.io/asp-net-core-2-2-health-checks-explained/
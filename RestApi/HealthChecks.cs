using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SqlTypes;
using System.Dynamic;
using System.Net.Http;
using Microsoft.Data.SqlClient;
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
    public static async Task<HealthExt> GetHealth(ILog log)
    {
        bool isHealthy = false, databaseIsHealthy = false, vaultIsHealthy = false;
        dynamic vaultInfo = null;
        int? timeoutMs = 1000; // let it be constant for now

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

        HealthExt.HealthComponent_.Vault_.VaultInfo_ info = null;
        if (vaultInfo != null)
            try {
                info = new()
                {
                    Initialized = vaultInfo.initialized,
                    Sealed = vaultInfo.@sealed,
                    StandBy = vaultInfo.standby,
                    PerformanceStandby = vaultInfo.performance_standby
                };
            } catch {
            }

        var result = new HealthExt
        {
            IsHealthy = isHealthy,
            Timestamp = DateTime.SpecifyKind(new SqlDateTime(DateTime.UtcNow).Value, DateTimeKind.Utc),
            Version = DeploymentInformationsConfiguration.Version,

            Component = new HealthExt.HealthComponent_
            {
                Database = new HealthExt.HealthComponent_.Database_
                {
                    IsHealthy = databaseIsHealthy
                },
                Vault = new HealthExt.HealthComponent_.Vault_
                {
                    IsHealthy = vaultIsHealthy,
                    Info = info
                }
            }
        };

        return result;
    }
}

public class SqlServerHealthCheck(SqlConnection connection) : IHealthCheck
{
    private readonly SqlConnection _connection = connection;

    private readonly Random _random = new();

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {

            //await DatabaseCanConnectAsync()
            await Task.Delay(1000);
            if (_random.NextDouble() < 0.15)
                return HealthCheckResult.Degraded();
        }
        catch (SqlException)
        {
            return HealthCheckResult.Unhealthy();
        }

        return HealthCheckResult.Healthy();
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

public class VaultHealthCheck(ILog log) : IHealthCheck
{
    private readonly ILog _log = log;

    private readonly Random _random = new();

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.Delay(1000);

            if (_random.NextDouble() < 0.25)
                return HealthCheckResult.Degraded();
        }
        catch (SqlException)
        {
            return HealthCheckResult.Unhealthy();
        }

        return HealthCheckResult.Healthy();
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
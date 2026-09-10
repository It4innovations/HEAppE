using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HEAppE.DataAccessTier;
using HEAppE.RestApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi.IntegrationTests.Infrastructure;

public class HEAppEWebApplicationFactory : WebApplicationFactory<Startup>
{
    private readonly string? _connectionString;

    public HEAppEWebApplicationFactory() : this(null)
    {
    }

    internal HEAppEWebApplicationFactory(string? connectionString)
    {
        _connectionString = connectionString;
    }

    private static bool _dbInitialized = false;
    private static readonly object _dbLock = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var envConnStr = Environment.GetEnvironmentVariable("ConnectionStrings__MiddlewareContext")
                ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            var connStr = _connectionString ?? envConnStr ?? TestCredentials.GetDefaultConnectionString();

            var testConfig = new Dictionary<string, string?>
            {
                { "ConnectionStrings:MiddlewareContext", connStr },
                { "DatabaseMigrationSettings:AutoMigrateDatabase", "true" },
                { "IpRateLimiting:EnableEndpointRateLimiting", "false" },
                { "JwtTokenIntrospectionConfiguration:IsEnabled", "false" },
                { "ExternalAuthenticationSettings:IsEnabled", "false" },
                { "VaultConnectorSettings:IsEnabled", "false" },
                { "BackGroundThreadSettings:ExternalServiceHealthMonitoringSettings:IsEnabled", "false" },
                { "ApplicationAPISettings:AllowedHosts:0", "http://localhost:5000" },
                { "ApplicationAPISettings:SwaggerDocSettings:PrefixDocPath", "swagger" },
                { "ApplicationAPISettings:SwaggerDocSettings:Title", "HEAppE Web API" },
                { "ExpirioSettings:BaseUrl", "http://localhost:5000" },
                { "ExpirioSettings:TimeoutSeconds", "10" }
            };

            config.AddInMemoryCollection(testConfig);

            // Locate and load seed.ci.njson
            string? seedPath = null;
            var cur = new DirectoryInfo(AppContext.BaseDirectory);
            while (cur != null)
            {
                var cand = Path.Combine(cur.FullName, "ci", "config", "seed.ci.njson");
                if (File.Exists(cand))
                {
                    seedPath = cand;
                    break;
                }
                cur = cur.Parent;
            }

            if (seedPath == null)
            {
                cur = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (cur != null)
                {
                    var cand = Path.Combine(cur.FullName, "ci", "config", "seed.ci.njson");
                    if (File.Exists(cand))
                    {
                        seedPath = cand;
                        break;
                    }
                    cur = cur.Parent;
                }
            }

            if (seedPath != null && File.Exists(seedPath))
            {
                config.AddNotJson(seedPath);
                var configRoot = config.Build();
                MiddlewareContextSettings.Clear();
                configRoot.Bind("MiddlewareContextSettings", new MiddlewareContextSettings());
            }
        });

        builder.ConfigureServices(services =>
        {
            lock (_dbLock)
            {
                if (!_dbInitialized)
                {
                    try
                    {
                        var envConnStr = Environment.GetEnvironmentVariable("ConnectionStrings__MiddlewareContext")
                            ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
                        var connStr = _connectionString ?? envConnStr ?? TestCredentials.GetDefaultConnectionString();
                        var masterConnStr = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connStr) { InitialCatalog = "master" }.ConnectionString;
                        using (var masterConn = new Microsoft.Data.SqlClient.SqlConnection(masterConnStr))
                        {
                            masterConn.Open();
                            using var cmd = masterConn.CreateCommand();
                            cmd.CommandText = "IF DB_ID('HEAppE_CI') IS NULL CREATE DATABASE HEAppE_CI;";
                            cmd.ExecuteNonQuery();
                        }

                        HEAppE.DataAccessTier.Configuration.DatabaseMigrationSettings.AutoMigrateDatabase = true;
                        MiddlewareContextSettings.ConnectionString = connStr;

                        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
                        var logger = loggerFactory.CreateLogger("HEAppE.DatabaseInitialization");
                        
                        using (var context = new MiddlewareContext(logger))
                        {
                            context.Database.Migrate();
                        }
                        
                        MiddlewareContext.InitializeDatabase(logger);
                        _dbInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TestFixture] Database init warning: {ex.Message} {ex.StackTrace}");
                    }
                }
            }
        });
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using HEAppE.BusinessLogicTier.Configuration;
using HEAppE.DataAccessTier.UnitOfWork;
using log4net;
using MicroKnights.Log4NetHelper;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using HEAppE.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HEAppE.RestApi;

public class Program
{
    public static void Main(string[] args)
    {
        var host = CreateWebHostBuilder(args).Build();

        using (var scope = host.Services.CreateScope())
        {
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

            // Configure log4net early so that seeding logs are captured in the log output.
            // Normally log4net is added in Startup.Configure(), which runs during host.Run() —
            // too late to capture InitializeDatabase logs.
            var localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
            loggerFactory.AddLog4Net(localRunEnv == "Docker"
                ? "Logging/log4netDocker.config"
                : "Logging/log4net.config");

            var loggingConnString = host.Services.GetRequiredService<IConfiguration>().GetConnectionString("Logging");
            AdoNetAppenderHelper.SetConnectionString(loggingConnString);

            var logger = loggerFactory.CreateLogger("HEAppE.DatabaseInitialization");

            if (!string.IsNullOrEmpty(loggingConnString))
            {
                // Run in a background thread so it doesn't block application startup
                _ = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        logger.LogInformation("Performing startup logging database maintenance...");
                        using (var conn = new Microsoft.Data.SqlClient.SqlConnection(loggingConnString))
                        {
                            conn.Open();
                            var dbName = conn.Database;
                            var cmdBuilder = new Microsoft.Data.SqlClient.SqlCommandBuilder();
                            var safeDbName = cmdBuilder.QuoteIdentifier(dbName);

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandText = $"ALTER DATABASE {safeDbName} SET RECOVERY SIMPLE;";
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandTimeout = 0; // Infinite timeout
                                cmd.CommandText = "IF OBJECT_ID('Log', 'U') IS NOT NULL " +
                                                  "BEGIN " +
                                                  "    DECLARE @Deleted INT; " +
                                                  "    SET @Deleted = 1; " +
                                                  "    WHILE (@Deleted > 0) " +
                                                  "    BEGIN " +
                                                  "        DELETE TOP (10000) FROM Log WHERE [Date] < DATEADD(day, -30, GETUTCDATE()); " +
                                                  "        SET @Deleted = @@ROWCOUNT; " +
                                                  "    END " +
                                                  "END";
                                cmd.ExecuteNonQuery();
                            }

                            using (var cmd = conn.CreateCommand())
                            {
                                cmd.CommandTimeout = 0; // Infinite timeout
                                cmd.CommandText = $"DBCC SHRINKDATABASE ({safeDbName}, 10) WITH NO_INFOMSGS;";
                                cmd.ExecuteNonQuery();
                            }
                        }
                        logger.LogInformation("Startup logging database maintenance completed successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Could not configure logging database on startup: {ex.Message}");
                    }
                });
            }

            try
            {
                logger.LogInformation("Checking database compatibility, migrations and seeding...");
                HEAppE.DataAccessTier.MiddlewareContext.InitializeDatabase(logger);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "An error occurred during database initialization.");
                throw;
            }
        }

        host.Run();
    }

    public static IWebHostBuilder CreateWebHostBuilder(string[] args)
    {
        IWebHostBuilder builder;
        var localRunEnv = Environment.GetEnvironmentVariable("ASPNETCORE_RUNTYPE_ENVIRONMENT");
        if (localRunEnv == "Docker")
            builder = WebHost.CreateDefaultBuilder()
                .UseUrls("http://*:80")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("/opt/heappe/confs/appsettings.json", false, true);
                    config.AddNotJson("/opt/heappe/confs/seed.njson");
                })
                .UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = long.MaxValue;
                    options.Limits.MinRequestBodyDataRate = null;
                    options.Limits.MinResponseDataRate = null;
                })
                .UseStartup<Startup>();
        else
            builder = WebHost.CreateDefaultBuilder()
                .UseUrls("http://*:5005")
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    if (!FileSystemUtils.AddConfigurationFiles(
                        confsDirs: [
                            Directory.GetCurrentDirectory(),
                            "/opt/heappe/confs",
                            "P:\\source\\localHEAppE\\confs"
                        ],
                        confFiles: [
                            ("appsettings.json", true),
                            ("seed.njson", true)
                        ],
                        addJsonFile: confPath => config.AddJsonFile(confPath, false, true),
                        addNotJson: confPath => config.AddNotJson(confPath))
                    )
                        throw new Exception("Configuration files not found!");
                })
                .UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = long.MaxValue;
                    options.Limits.MinRequestBodyDataRate = null;
                    options.Limits.MinResponseDataRate = null;
                })
                .UseStartup<Startup>();
        return builder;
    }
}

#region NJSON Parser Methods

public static class ConfigurationExtensionMethods
{
    public static IConfigurationBuilder AddNotJson(this IConfigurationBuilder builder, string path)
    {
        var releaseConfigJson = File.ReadAllText(path);
        releaseConfigJson = releaseConfigJson.Replace(@"\", @"\\");

        var dictionary = JsonConfigurationParser.Parse(releaseConfigJson);
        return builder.AddInMemoryCollection(dictionary);
    }
}

public class JsonConfigurationParser
{
    private JsonConfigurationParser()
    {
    }

    private readonly IDictionary<string, string> _data =
        new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Stack<string> _context = new();
    private string _currentPath;

    public static IDictionary<string, string> Parse(string json)
    {
        return new JsonConfigurationParser().ParseJson(json);
    }

    private IDictionary<string, string> ParseJson(string json)
    {
        _data.Clear();

        var jsonConfig = JObject.Parse(json);

        VisitJObject(jsonConfig);

        return _data;
    }

    private void VisitJObject(JObject jObject)
    {
        foreach (var property in jObject.Properties())
        {
            EnterContext(property.Name);
            VisitProperty(property);
            ExitContext();
        }
    }

    private void VisitProperty(JProperty property)
    {
        VisitToken(property.Value);
    }

    private void VisitToken(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
                VisitJObject(token.Value<JObject>());
                break;

            case JTokenType.Array:
                VisitArray(token.Value<JArray>());
                break;

            case JTokenType.Integer:
            case JTokenType.Float:
            case JTokenType.String:
            case JTokenType.Boolean:
            case JTokenType.Bytes:
            case JTokenType.Raw:
            case JTokenType.Null:
                VisitPrimitive(token.Value<JValue>());
                break;

            default:
                throw new FormatException("Unsupported JSON token");
        }
    }

    private void VisitArray(JArray array)
    {
        for (var index = 0; index < array.Count; index++)
        {
            EnterContext(index.ToString());
            VisitToken(array[index]);
            ExitContext();
        }
    }

    private void VisitPrimitive(JValue data)
    {
        var key = _currentPath;

        if (_data.ContainsKey(key)) throw new FormatException("Duplicate Key");

        _data[key] = data.Value != null
            ? data.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    private void EnterContext(string context)
    {
        _context.Push(context);
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    private void ExitContext()
    {
        _context.Pop();
        _currentPath = ConfigurationPath.Combine(_context.Reverse());
    }

    #endregion
}
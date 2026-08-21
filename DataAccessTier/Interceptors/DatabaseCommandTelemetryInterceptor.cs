using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DomainObjects.Monitoring;
using HEAppE.Services.Monitoring;
using HEAppE.Utils;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HEAppE.DataAccessTier.Interceptors;

/// <summary>
/// EF Core DbCommandInterceptor that records query duration and errors to the telemetry service.
/// Filters out fast micro-queries (< MinDbDurationThresholdMs) and ignores self-inserts into ExternalServiceHealthLog.
/// </summary>
public class DatabaseCommandTelemetryInterceptor : DbCommandInterceptor
{
    private static readonly Lazy<DatabaseCommandTelemetryInterceptor> _instance =
        new(() => new DatabaseCommandTelemetryInterceptor());

    public static DatabaseCommandTelemetryInterceptor Instance => _instance.Value;

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, null);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, null);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, null);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, null);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, null);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object result,
        CancellationToken cancellationToken = default)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, null);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ProcessCommandExecuted(command, eventData.Duration.TotalMilliseconds, eventData.Exception);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private static void ProcessCommandExecuted(DbCommand command, double durationMs, Exception exception)
    {
        try
        {
            var telemetryService = ExternalServiceTelemetryService.Instance;
            if (telemetryService?.Settings?.IsEnabled != true) return;

            var cmdText = command?.CommandText;
            if (string.IsNullOrEmpty(cmdText)) return;

            // Avoid logging self-telemetry insertions to prevent recursion
            if (cmdText.Contains("ExternalServiceHealthLog", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var duration = (long)Math.Round(durationMs);
            var isSuccess = exception == null;

            // Ignore successful queries faster than threshold
            if (isSuccess && duration < telemetryService.Settings.MinDbDurationThresholdMs)
            {
                return;
            }

            var operation = ExtractOperationSummary(cmdText);
            var sqlErrorNumber = (exception as Microsoft.Data.SqlClient.SqlException)?.Number.ToString();

            telemetryService.Record(new ExternalServiceHealthLog
            {
                ServiceName = "database",
                ServiceType = "database",
                Protocol = "sql",
                EndpointOrHost = "database",
                CommandOrPath = operation,
                ResponseTimeMs = duration,
                IsAvailable = isSuccess,
                StatusCode = sqlErrorNumber ?? (isSuccess ? "0" : "-1"),
                ErrorMessage = exception?.Message,
                JobId = JobExecutionContext.CurrentJobId,
                TaskId = JobExecutionContext.CurrentTaskId,
                ClusterId = JobExecutionContext.CurrentClusterId,
                RequestId = JobExecutionContext.CurrentRequestId,
                Source = "Execution",
                Timestamp = DateTime.UtcNow
            });
        }
        catch
        {
            // Suppress any telemetry recording errors to protect DB operations
        }
    }

    private static string ExtractOperationSummary(string cmdText)
    {
        var text = cmdText.Trim();
        var firstLine = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
        if (firstLine.Length > 200)
        {
            return firstLine.Substring(0, 197) + "...";
        }
        return firstLine;
    }
}

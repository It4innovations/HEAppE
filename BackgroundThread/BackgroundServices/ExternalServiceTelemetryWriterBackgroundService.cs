using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DataAccessTier.UnitOfWork;
using HEAppE.DomainObjects.Monitoring;
using HEAppE.Services.Monitoring;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HEAppE.BackgroundThread.BackgroundServices;

/// <summary>
/// Background worker consuming telemetry items from the non-blocking channel and flushing them in batches to the database.
/// Also periodically purges telemetry logs older than the retention threshold.
/// </summary>
internal class ExternalServiceTelemetryWriterBackgroundService : BackgroundService
{
    private readonly IExternalServiceTelemetryService _telemetryService;
    private readonly ILogger _logger;
    private DateTime _lastPurgeTime = DateTime.MinValue;

    public ExternalServiceTelemetryWriterBackgroundService(
        IExternalServiceTelemetryService telemetryService,
        ILoggerFactory loggerFactory)
    {
        _telemetryService = telemetryService ?? ExternalServiceTelemetryService.Instance;
        _logger = loggerFactory.CreateLogger(
            "HEAppE.BackgroundThread.BackgroundServices.ExternalServiceTelemetryWriterBackgroundService");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        _logger.LogInformation("ExternalServiceTelemetryWriter background service started.");

        var batch = new List<ExternalServiceHealthLog>();
        var settings = _telemetryService.Settings;
        var flushInterval = TimeSpan.FromMilliseconds(Math.Max(500, settings.FlushIntervalMs));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(flushInterval);

                try
                {
                    await foreach (var item in _telemetryService.ReadAllAsync(timeoutCts.Token))
                    {
                        batch.Add(item);
                        if (batch.Count >= settings.MaxBatchSize)
                        {
                            await FlushBatchAsync(batch);
                        }
                    }
                }
                catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
                {
                    // Timeout reached - flush current batch
                }

                if (batch.Count > 0)
                {
                    await FlushBatchAsync(batch);
                }

                // Check periodic purging (once every hour)
                if (DateTime.UtcNow - _lastPurgeTime > TimeSpan.FromHours(1))
                {
                    await PurgeStaleLogsAsync(settings.RetentionDays);
                    _lastPurgeTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error while processing or flushing telemetry logs.");
                await Task.Delay(1000, stoppingToken);
            }
        }

        // Final flush on graceful shutdown
        if (batch.Count > 0)
        {
            try
            {
                await FlushBatchAsync(batch);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during final telemetry batch flush on shutdown.");
            }
        }

        _logger.LogInformation("ExternalServiceTelemetryWriter background service stopped.");
    }

    private async Task FlushBatchAsync(List<ExternalServiceHealthLog> batch)
    {
        if (batch.Count == 0) return;

        var itemsToWrite = batch.ToArray();
        batch.Clear();

        try
        {
            using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
            await unitOfWork.ExternalServiceHealthLogRepository.BulkInsertAsync(itemsToWrite);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist a batch of {Count} telemetry records to the database.", itemsToWrite.Length);
        }
    }

    private async Task PurgeStaleLogsAsync(int retentionDays)
    {
        if (retentionDays <= 0) return;

        try
        {
            var threshold = DateTime.UtcNow.AddDays(-retentionDays);
            using IUnitOfWork unitOfWork = new DatabaseUnitOfWork(_logger);
            await unitOfWork.ExternalServiceHealthLogRepository.DeleteOlderThanAsync(threshold);
            _logger.LogInformation("Purged telemetry logs older than {Threshold} (retention: {RetentionDays} days).", threshold, retentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error purging old telemetry logs.");
        }
    }
}

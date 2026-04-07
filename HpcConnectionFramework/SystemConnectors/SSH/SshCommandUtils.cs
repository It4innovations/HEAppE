using System;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
/// Utility class for executing SSH commands with optimized retry logic.
/// </summary>
internal static class SshCommandUtils
{    
    /// <summary>
    /// Maximum number of attempts for a single command execution.
    /// Loaded from configuration (default is usually 10).
    /// </summary>
    private static readonly int MaxRetries = HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts;
    
    /// <summary>
    /// Base delay for exponential backoff in milliseconds.
    /// Using a lower value (1s) than the connection timeout (30s) to speed up recovery.
    /// </summary>
    private const int CommandRetryBaseDelayMs = 1000; 

    static SshCommandUtils()
    {
    }


    /// <summary>
    /// Runs an SSH command asynchronously with automatic retries.
    /// </summary>
    internal static async Task<SshCommandWrapper> RunSshCommandAsync(object client, string command, ILogger logger)
    {
        var adapter = new SshClientAdapter((Renci.SshNet.SshClient)client);
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                return await ExecuteInternalAsync(adapter, command, attempt, logger);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxRetries)
            {
                int delay = (int)Math.Pow(2, attempt - 1) * CommandRetryBaseDelayMs;
                delay = Math.Min(delay, 10000); 
                
                logger.LogWarning("SSH transient error on attempt {Attempt}/{MaxRetries}. Retrying in {Delay}ms. Error: {Error}", attempt, MaxRetries, delay, ex.Message);
                
                await Task.Delay(delay);
            }
            catch (Exception ex)
            {
                logger.LogError("SSH command failed after {Attempt} attempts. Error: {Error}, Command: {Command}", attempt, ex.Message, command);
                throw;
            }
        }
    }


    /// <summary>
    /// Internal async execution logic.
    /// </summary>
    private static async Task<SshCommandWrapper> ExecuteInternalAsync(SshClientAdapter client, string command, int attempt, ILogger logger)
    {
        logger.LogInformation("Executing SSH command async (Attempt {Attempt}). Command: {Command}, Client: {Client}", attempt, command, client);
        
        var startTime = DateTime.UtcNow;
        var sshCommand = await client.RunCommandAsync(command); 
        var duration = DateTime.UtcNow - startTime;

        logger.LogInformation("SSH command executed async. Command: {Command}, Duration: {Duration}ms, Exit Code: {ExitCode}", command, duration.TotalMilliseconds, sshCommand.ExitStatus);
        
        return ProcessResult(sshCommand, logger);
    }

    private static SshCommandWrapper ProcessResult(SshCommandWrapper sshCommand, ILogger logger)
    {
        // Handle specific error cases that should NOT be retried (permanent failures)
        if (sshCommand.ExitStatus != 0)
        {
            if (sshCommand.Error.Contains("No such file or directory"))
            {
                logger.LogWarning("SSH command error (No such file or directory). Error: {Error}, Exit Code: {ExitCode}", sshCommand.Error, sshCommand.ExitStatus);
                throw new InputValidationException("NoFileOrDirectory");
            }

            if (sshCommand.Error.Contains("GIT CLONE ERROR"))
            {
                logger.LogWarning("SSH command error (git clone). Error: {Error}, Exit Code: {ExitCode}", sshCommand.Error, sshCommand.ExitStatus);
                throw new InputValidationException("GitCloneCommandError");
            }

            // General command failure - throw exception. IsTransient will determine if we retry.
            logger.LogWarning("SSH command execution failed with non-zero exit code. Error: {Error}, Exit Code: {ExitCode}", sshCommand.Error, sshCommand.ExitStatus);
            throw new SshCommandException(sshCommand.Error, sshCommand.ExitStatus, sshCommand.CommandText);
        }

        // Log warnings if stderr has content even with ExitCode 0
        if (!string.IsNullOrEmpty(sshCommand.Error))
        {
            logger.LogWarning("SSH command finished with warnings. Error: {Error}, Command: {Command}", sshCommand.Error, sshCommand.CommandText);
        }

        if (!string.IsNullOrEmpty(sshCommand.Result))
        {
            // Truncate extremely large outputs for standard logging to prevent memory pressure and system delays
            // Admin can still access full results via detailed diagnostic logs if needed or directly from the command result object.
            const int maxLogOutputLength = 2000;
            string logOutput = sshCommand.Result.Length > maxLogOutputLength 
                ? sshCommand.Result.Substring(0, maxLogOutputLength) + "... [TRUNCATED]" 
                : sshCommand.Result;
            
            logger.LogDebug("SSH command output (first {Length} chars): {Output}", logOutput.Length, logOutput);
        }

        return sshCommand;
    }

    /// <summary>
    /// Identifies if the exception is a transient network/timeout error that warrants a retry.
    /// </summary>
    private static bool IsTransient(Exception ex)
    {
        // Identify network-level issues where a retry might succeed
        return ex is System.Net.Sockets.SocketException || 
               ex is TimeoutException || 
               ex is Renci.SshNet.Common.SshConnectionException ||
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("Failed to open a channel", StringComparison.OrdinalIgnoreCase);
    }
}
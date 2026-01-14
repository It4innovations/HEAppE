using System;
using System.Threading;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using log4net;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
/// Utility class for executing SSH commands with optimized retry logic.
/// </summary>
internal static class SshCommandUtils
{
    private static readonly ILog _log;
    
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
        _log = LogManager.GetLogger(typeof(SshCommandUtils));
    }

    /// <summary>
    /// Runs an SSH command with automatic retries on transient network failures or timeouts.
    /// </summary>
    /// <param name="client">The SSH client adapter instance.</param>
    /// <param name="command">The shell command to execute.</param>
    /// <returns>A wrapper containing the command results (ExitStatus, Result, Error).</returns>
    /// <exception cref="InputValidationException">Thrown for known permanent errors like 'File not found'.</exception>
    /// <exception cref="SshCommandException">Thrown when the command fails or retries are exhausted.</exception>
    internal static SshCommandWrapper RunSshCommand(SshClientAdapter client, string command)
    {
        int attempt = 0;

        while (true)
        {
            attempt++;
            try
            {
                return ExecuteInternal(client, command, attempt);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxRetries)
            {
                // Exponential backoff strategy: 1s, 2s, 4s, 8s... capped at 10 seconds.
                // We don't use the full ConnectionTimeout (30s) here to keep the system responsive.
                int delay = (int)Math.Pow(2, attempt - 1) * CommandRetryBaseDelayMs;
                delay = Math.Min(delay, 10000); 
                
                _log.Warn($"SSH transient error on attempt {attempt}/{MaxRetries}. " +
                          $"Retrying in {delay}ms. Error: {ex.Message}");
                
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Internal execution logic including logging and exit status validation.
    /// </summary>
    private static SshCommandWrapper ExecuteInternal(SshClientAdapter client, string command, int attempt)
    {
        _log.Info($"Executing SSH command (Attempt {attempt}). Command: {command}, Client: {client}");
        
        var startTime = DateTime.UtcNow;
        // This call performs the actual synchronous network I/O
        var sshCommand = client.RunCommand(command); 
        var duration = DateTime.UtcNow - startTime;

        _log.Info($"SSH command executed. Command: {command}, Duration: {duration.TotalMilliseconds}ms, Exit Code: {sshCommand.ExitStatus}");

        // Handle specific error cases that should NOT be retried (permanent failures)
        if (sshCommand.ExitStatus != 0)
        {
            if (sshCommand.Error.Contains("No such file or directory"))
            {
                _log.Warn($"SSH command error (No such file or directory). Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}");
                throw new InputValidationException("NoFileOrDirectory");
            }

            if (sshCommand.Error.Contains("GIT CLONE ERROR"))
            {
                _log.Warn($"SSH command error (git clone). Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}");
                throw new InputValidationException("GitCloneCommandError");
            }

            // General command failure - throw exception to prevent automatic retry of logic errors
            _log.Error($"SSH command failed. Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}");
            throw new SshCommandException(sshCommand.Error, sshCommand.ExitStatus, sshCommand.CommandText);
        }

        // Log warnings if stderr has content even with ExitCode 0
        if (!string.IsNullOrEmpty(sshCommand.Error))
        {
            _log.Warn($"SSH command finished with warnings. Error: {sshCommand.Error}, Command: {sshCommand.CommandText}");
        }

        if (!string.IsNullOrEmpty(sshCommand.Result))
        {
            _log.Debug($"SSH command output: {sshCommand.Result}");
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
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }
}
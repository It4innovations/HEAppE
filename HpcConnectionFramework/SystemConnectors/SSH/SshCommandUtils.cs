using System;
using System.Threading;
using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using log4net;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
/// Utility class for executing SSH commands with retry logic.
/// </summary>
internal static class SshCommandUtils
{
    private static readonly ILog _log;
    private static readonly int MaxRetries = HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionRetryAttempts;
    private static readonly int BaseDelayMs = HPCConnectionFrameworkConfiguration.SshClientSettings.ConnectionTimeout;

    static SshCommandUtils()
    {
        _log = LogManager.GetLogger(typeof(SshCommandUtils));
    }

    /// <summary>
    /// Runs an SSH command with automatic retries on network failures or timeouts.
    /// </summary>
    /// <param name="client">The SSH client adapter instance.</param>
    /// <param name="command">The shell command to execute.</param>
    /// <returns>A wrapper containing the command results.</returns>
    /// <exception cref="InputValidationException">Thrown when the command fails due to invalid input or missing files.</exception>
    /// <exception cref="SshCommandException">Thrown when the command execution fails after all retries.</exception>
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
                // Calculate exponential backoff: 2s, 4s, 8s...
                int delay = (int)Math.Pow(2, attempt - 1) * BaseDelayMs;
                
                _log.Warn($"SSH transient error on attempt {attempt}/{MaxRetries}. Retrying in {delay}ms. Error: {ex.Message}");
                
                Thread.Sleep(delay);
            }
        }
    }

    /// <summary>
    /// Internal execution logic including logging and exit status validation.
    /// </summary>
    private static SshCommandWrapper ExecuteInternal(SshClientAdapter client, string command, int attempt)
    {
        _log.Info($"Running SSH command (Attempt {attempt}). Command: {command}, Client: {client}");
        
        var startTime = DateTime.UtcNow;
        // This call might throw TimeoutException or SshException depending on the adapter implementation
        var sshCommand = client.RunCommand(command); 
        var duration = DateTime.UtcNow - startTime;

        _log.Info($"SSH command executed. Command: {command}, Duration: {duration.TotalMilliseconds}ms, Exit Code: {sshCommand.ExitStatus}");

        // Handle specific error cases that should not be retried
        if (sshCommand.ExitStatus != 0)
        {
            if (sshCommand.Error.Contains("No such file or directory"))
            {
                _log.Warn($"SSH command error (No such file or directory). Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}, Command: {sshCommand.CommandText}");
                throw new InputValidationException("NoFileOrDirectory");
            }

            if (sshCommand.Error.Contains("GIT CLONE ERROR"))
            {
                _log.Warn($"SSH command error (git clone). Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}, Command: {sshCommand.CommandText}");
                throw new InputValidationException("GitCloneCommandError");
            }

            _log.Error($"SSH command failed. Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}, Command: {sshCommand.CommandText}");
            throw new SshCommandException(sshCommand.Error, sshCommand.ExitStatus, sshCommand.CommandText);
        }

        // Log warnings if stderr is not empty despite exit code 0
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
    /// Determines if an exception is a transient network/timeout error that warrants a retry.
    /// </summary>
    private static bool IsTransient(Exception ex)
    {
        // Add specific exceptions thrown by your SSH library (e.g., Renci.SshNet.Common.SshException)
        return ex is System.Net.Sockets.SocketException || 
               ex is TimeoutException || 
               ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
    }
}
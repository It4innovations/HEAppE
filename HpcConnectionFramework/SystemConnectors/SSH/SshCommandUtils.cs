using HEAppE.Exceptions.External;
using HEAppE.Exceptions.Internal;
using log4net;
using System;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh command utils
/// </summary>
internal static class SshCommandUtils
{
    #region Properties

    /// <summary>
    ///     Log4Net logger
    /// </summary>
    private static readonly ILog _log;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    static SshCommandUtils()
    {
        _log = LogManager.GetLogger(typeof(SshCommandUtils));
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Run ssh command
    /// </summary>
    /// <param name="client">Client</param>
    /// <param name="command">Command</param>
    /// <returns></returns>
    internal static SshCommandWrapper RunSshCommand(SshClientAdapter client, string command)
    {
        _log.Info($"Running SSH command. Command: {command}, Client: {client}");
        var startTime = DateTime.UtcNow;
        var sshCommand = client.RunCommand(command);
        var duration = DateTime.UtcNow - startTime;
        _log.Info($"SSH command executed. Command: {command}, Duration: {duration.TotalMilliseconds}ms, Exit Code: {sshCommand.ExitStatus}");

        if (sshCommand.ExitStatus != 0)
        {
            if (sshCommand.Error.Contains("No such file or directory"))
            {
                _log.Warn($"SSH command error (No such file or directory). Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}, Command: {sshCommand.CommandText}, Duration: {duration.TotalMilliseconds}ms");
                throw new InputValidationException("NoFileOrDirectory");
            }

            if (sshCommand.Error.Contains("GIT CLONE ERROR"))
            {
                _log.Warn($"SSH command error (git clone). Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}, Command: {sshCommand.CommandText}, Duration: {duration.TotalMilliseconds}ms");
                throw new InputValidationException("GitCloneCommandError");
            }

            _log.Error($"SSH command failed. Error: {sshCommand.Error}, Exit Code: {sshCommand.ExitStatus}, Command: {sshCommand.CommandText}, Duration: {duration.TotalMilliseconds}ms");
            throw new SshCommandException(sshCommand.Error, sshCommand.ExitStatus, sshCommand.CommandText);
        }

        if (!string.IsNullOrEmpty(sshCommand.Error))
        {
            _log.Warn($"SSH command finished with warnings/errors. Error: {sshCommand.Error}, Command: {sshCommand.CommandText}, Duration: {duration.TotalMilliseconds}ms");
        }

        if (!string.IsNullOrEmpty(sshCommand.Result))
        {
            _log.Debug($"SSH command output: {sshCommand.Result}");
        }

        return sshCommand;
    }


    #endregion
}
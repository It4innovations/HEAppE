using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh command wrapper
/// </summary>
public class SshCommandWrapper
{
    #region Instances

    private int _exitStatus;
    private string _error;
    private string _result;
    private string _commandText;

    #endregion

    /// <summary>
    ///     Command exit status
    /// </summary>
    public int ExitStatus
    {
        get => _exitStatus;
        internal set => _exitStatus = value;
    }

    /// <summary>
    ///     Command result error message
    /// </summary>
    public string Error
    {
        get => _error;
        internal set => _error = value;
    }

    /// <summary>
    ///     Command result
    /// </summary>
    public string Result
    {
        get => _result;
        internal set => _result = value;
    }

    /// <summary>
    ///     Command text
    /// </summary>
    public string CommandText
    {
        get => _commandText;
        internal set => _commandText = value;
    }

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public SshCommandWrapper()
    {
        _exitStatus = 0;
        _error = string.Empty;
        _result = string.Empty;
        _commandText = string.Empty;
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="sshCommand">SSh command</param>
    public SshCommandWrapper(SshCommand sshCommand)
    {
        _exitStatus = sshCommand.ExitStatus ?? 0;
        _error = string.IsNullOrEmpty(sshCommand.Error) ? string.Empty : sshCommand.Error;
        _result = string.IsNullOrEmpty(sshCommand.Result) ? string.Empty : sshCommand.Result;
        _commandText = string.IsNullOrEmpty(sshCommand.CommandText) ? string.Empty : sshCommand.CommandText;
        sshCommand.Dispose();
    }

    #endregion
}
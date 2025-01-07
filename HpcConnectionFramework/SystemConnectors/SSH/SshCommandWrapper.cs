using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH;

/// <summary>
///     Ssh command wrapper
/// </summary>
public class SshCommandWrapper
{
    #region Instances

    private readonly SshCommand _sshCommand;
    private int _exitStatus;
    private string _error;
    private string _result;
    private string _commandText;

    #endregion

    #region Properties

    /// <summary>
    ///     Command exit status
    /// </summary>
    public int ExitStatus
    {
        get => _sshCommand?.ExitStatus ?? _exitStatus;
        internal set => _exitStatus = value;
    }

    /// <summary>
    ///     Command result error message
    /// </summary>
    public string Error
    {
        get => _sshCommand?.Error ?? _error;
        internal set => _error = value;
    }

    /// <summary>
    ///     Command result
    /// </summary>
    public string Result
    {
        get => _sshCommand?.Result ?? _result;
        internal set => _result = value;
    }

    /// <summary>
    ///     Command text
    /// </summary>
    public string CommandText
    {
        get => _sshCommand?.CommandText ?? _commandText;
        internal set => _commandText = value;
    }

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public SshCommandWrapper()
    {
        _sshCommand = null;
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
        _sshCommand = sshCommand;
    }

    #endregion
}
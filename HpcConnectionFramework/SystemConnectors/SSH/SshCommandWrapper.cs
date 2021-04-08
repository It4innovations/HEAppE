using Renci.SshNet;

namespace HEAppE.HpcConnectionFramework.SystemConnectors.SSH
{
    public class SshCommandWrapper
    {
        private readonly SshCommand _sshCommand;
        private int _exitStatus = 0;
        private string _error;
        private string _result;
        private string _commandText;

        public int ExitStatus { get => _sshCommand?.ExitStatus ?? _exitStatus; internal set => _exitStatus = value; }
        public string Error { get => _sshCommand?.Error ?? _error; internal set => _error = value; }
        public string Result { get => _sshCommand?.Result ?? _result; internal set => _result = value; }
        public string CommandText { get => _sshCommand?.CommandText ?? _commandText; internal set => _commandText = value; }

        public SshCommandWrapper(SshCommand sshCommand)
        {
            _sshCommand = sshCommand;
        }

        public SshCommandWrapper()
        {
            _sshCommand = null;
            _exitStatus = 0;
            _error = string.Empty;
            _result = string.Empty;
            _commandText = string.Empty;
        }
    }
}

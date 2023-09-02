using System;

namespace Exceptions.Internal
{
    public class SshCommandException : InternalException
    {
        public SshCommandException(string message) : base(message) { }

        public SshCommandException(string message, params object[] args) : base(message, args) { }

        public SshCommandException(string message, Exception innerException) : base(message, innerException) { }
    }
}

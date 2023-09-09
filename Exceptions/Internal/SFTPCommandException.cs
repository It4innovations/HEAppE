using System;

namespace Exceptions.Internal
{
    public class SFTPCommandException : InternalException
    {
        public SFTPCommandException(string message) : base(message) { }
        public SFTPCommandException(string message, params object[] args) : base(message, args) { }
        public SFTPCommandException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }
        public SFTPCommandException(string message, Exception innerException) : base(message, innerException) { }
    }
}

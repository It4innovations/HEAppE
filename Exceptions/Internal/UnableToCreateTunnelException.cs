using System;

namespace Exceptions.Internal
{
    public class UnableToCreateTunnelException : InternalException
    {
        public UnableToCreateTunnelException(string message) : base(message) { }

        public UnableToCreateTunnelException(string message, params object[] args) : base(message, args) { }

        public UnableToCreateTunnelException(string message, Exception innerException) : base(message, innerException) { }
    }
}

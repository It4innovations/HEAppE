using System;

namespace Exceptions.External
{
    public class UnableToCreateConnectionException : ExternalException
    {
        public UnableToCreateConnectionException(string message) : base(message) { }
        public UnableToCreateConnectionException(string message, params object[] args) : base(message, args) { }
        public UnableToCreateConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}

using System;

namespace Exceptions.Internal
{
    public class AuthenticationTypeException : InternalException
    {
        public AuthenticationTypeException(string message) : base(message) { }
        public AuthenticationTypeException(string message, params object[] args) : base(message, args) { }
        public AuthenticationTypeException(string message, Exception innerException) : base(message, innerException) { }
    }
}

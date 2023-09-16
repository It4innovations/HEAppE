using System;

namespace Exceptions.External
{
    public class AuthenticationCredentialsException : ExternalException
    {
        public AuthenticationCredentialsException(string message) : base(message) { }
        public AuthenticationCredentialsException(string message, params object[] args) : base(message, args) { }

        public AuthenticationCredentialsException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }

        public AuthenticationCredentialsException(string message, Exception innerException) : base(message, innerException) { }
    }
}

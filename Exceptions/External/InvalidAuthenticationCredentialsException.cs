using System;

namespace Exceptions.External
{
	public class InvalidAuthenticationCredentialsException : ExternalException {
		public InvalidAuthenticationCredentialsException(string message) : base(message) { }

        public InvalidAuthenticationCredentialsException(string message, params object[] args) : base(message, args) { }

        public InvalidAuthenticationCredentialsException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }

        public InvalidAuthenticationCredentialsException(string message, Exception innerException) : base(message, innerException) { }
    }
}
using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class AuthenticatedUserAlreadyDeletedException : ExternalException {
		public AuthenticatedUserAlreadyDeletedException(string message) : base(message) { }

        public AuthenticatedUserAlreadyDeletedException(string message, params object[] args) : base(message, args) { }

        public AuthenticatedUserAlreadyDeletedException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }

        public AuthenticatedUserAlreadyDeletedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
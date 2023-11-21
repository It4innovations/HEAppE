using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class AuthenticationTypeException : ExternalException
    {
        public AuthenticationTypeException(string message) : base(message) { }
        public AuthenticationTypeException(string message, params object[] args) : base(message, args) { }

        public AuthenticationTypeException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }

        public AuthenticationTypeException(string message, Exception innerException) : base(message, innerException) { }
    }
}

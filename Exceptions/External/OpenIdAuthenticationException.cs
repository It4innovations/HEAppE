using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class OpenIdAuthenticationException : ExternalException
    {
        public OpenIdAuthenticationException(string message) : base(message) { }

        public OpenIdAuthenticationException(string message, Exception innerException) : base(message, innerException) { }

        public OpenIdAuthenticationException(string message, params object[] args) : base(message, args) { }

        public OpenIdAuthenticationException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }
    }
}
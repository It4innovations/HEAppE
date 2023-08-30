using System;

namespace Exceptions.External
{
    public class OpenIdAuthenticationException : ExternalException
    {
        public OpenIdAuthenticationException(string message) : base(message)
        {
        }

        public OpenIdAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
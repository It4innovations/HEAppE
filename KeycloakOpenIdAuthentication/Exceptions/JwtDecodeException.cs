using System;

namespace HEAppE.KeycloakOpenIdAuthentication.Exceptions
{
    /// <summary>
    /// Exception thrown when unable to parse JWT token.
    /// </summary>
    public class JwtDecodeException : Exception
    {
        internal JwtDecodeException(string message) : base(message)
        {
        }
        internal JwtDecodeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

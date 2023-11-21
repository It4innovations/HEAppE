using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    /// <summary>
    /// KeyCloak Exception
    /// </summary>
    public class KeycloakOpenIdException : ExternalException
    {
        public KeycloakOpenIdException(string message) : base(message) { }
        public KeycloakOpenIdException(string message, params object[] args) : base(message, args) { }
        public KeycloakOpenIdException(string message, Exception innerException) : base(message, innerException) { }
    }
}

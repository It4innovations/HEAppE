using System;
using HEAppE.RestUtils.Interfaces;

namespace HEAppE.KeycloakOpenIdAuthentication.Exceptions
{
    public class KeycloakOpenIdException : ExceptionWithMessageAndInnerException
    {
        public KeycloakOpenIdException(string message) : base(message)
        {
        }
        public KeycloakOpenIdException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
using System;
using System.Runtime.Serialization;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement
{
    [Serializable]
    internal class KeycloakOpenIdException : Exception
    {
        public KeycloakOpenIdException()
        {
        }

        public KeycloakOpenIdException(string message) : base(message)
        {
        }

        public KeycloakOpenIdException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected KeycloakOpenIdException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
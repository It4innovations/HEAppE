using System;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions
{
    public class OpenIdAuthenticationException : ExternallyVisibleException
    {
        public OpenIdAuthenticationException(string message) : base(message)
        {
        }

        public OpenIdAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
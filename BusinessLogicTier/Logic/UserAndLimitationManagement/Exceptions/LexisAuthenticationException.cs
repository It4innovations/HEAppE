using System;

namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions
{
  public class LexisAuthenticationException : ExternallyVisibleException
    {
        public LexisAuthenticationException(string message) : base(message)
        {
        }

        public LexisAuthenticationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
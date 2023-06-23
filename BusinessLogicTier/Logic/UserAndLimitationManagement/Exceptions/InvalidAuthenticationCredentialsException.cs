namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions
{
    public class InvalidAuthenticationCredentialsException : ExternallyVisibleException
    {
        public InvalidAuthenticationCredentialsException(string message) : base(message) { }
    }
}
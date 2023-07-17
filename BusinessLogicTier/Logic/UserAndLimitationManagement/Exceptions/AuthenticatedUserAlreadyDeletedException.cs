namespace HEAppE.BusinessLogicTier.Logic.UserAndLimitationManagement.Exceptions
{
    public class AuthenticatedUserAlreadyDeletedException : ExternallyVisibleException
    {
        public AuthenticatedUserAlreadyDeletedException(string message) : base(message) { }
    }
}
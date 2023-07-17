namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions
{
    public class InvalidRequestException : ExternallyVisibleException
    {
        public InvalidRequestException(string message) : base(message) { }
    }
}
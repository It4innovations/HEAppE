namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions
{
    public class RequestedObjectDoesNotExistException : ExternallyVisibleException
    {
        public RequestedObjectDoesNotExistException(string message) : base(message) { }
    }
}

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Exceptions
{
    public class RequestedJobResourcesExceededUserLimitationsException : ExternallyVisibleException
    {
        public RequestedJobResourcesExceededUserLimitationsException(string message) : base(message) { }
    }
}
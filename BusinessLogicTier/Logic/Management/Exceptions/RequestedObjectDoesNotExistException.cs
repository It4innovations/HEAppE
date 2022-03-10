using System;

namespace HEAppE.BusinessLogicTier.Logic.Management.Exceptions
{
    internal class RequestedObjectDoesNotExistException : ExternallyVisibleException
    {
        public RequestedObjectDoesNotExistException(string message) : base(message) { }
    }
}

using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class RequestedObjectDoesNotExistException : ExternalException {
		public RequestedObjectDoesNotExistException(string message) : base(message) {}
        public RequestedObjectDoesNotExistException(string message, params object[] args) : base(message, args) { }
    }
}

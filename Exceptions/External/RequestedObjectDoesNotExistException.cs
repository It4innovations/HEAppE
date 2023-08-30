namespace Exceptions.External {
	public class RequestedObjectDoesNotExistException : ExternalException {
		public RequestedObjectDoesNotExistException(string message) : base(message) {}
	}
}

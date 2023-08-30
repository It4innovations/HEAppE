namespace Exceptions.External
{
	public class AuthenticatedUserAlreadyDeletedException : ExternalException {
		public AuthenticatedUserAlreadyDeletedException(string message) : base(message) {}
	}
}
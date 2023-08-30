namespace Exceptions.External
{
	public class InvalidAuthenticationCredentialsException : ExternalException {
		public InvalidAuthenticationCredentialsException(string message) : base(message) {}
	}
}
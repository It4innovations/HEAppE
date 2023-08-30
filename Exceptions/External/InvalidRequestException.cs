namespace Exceptions.External
{
	public class InvalidRequestException : ExternalException
	{
		public InvalidRequestException(string message) : base(message) { }
	}
}
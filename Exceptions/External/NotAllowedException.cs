namespace Exceptions.External
{
	public class NotAllowedException : ExternalException {
        public NotAllowedException(string message) : base(message) { }
	}
}
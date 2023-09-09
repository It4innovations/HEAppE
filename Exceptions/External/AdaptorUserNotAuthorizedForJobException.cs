using System;

namespace Exceptions.External
{
	public class AdaptorUserNotAuthorizedForJobException : ExternalException {
		public AdaptorUserNotAuthorizedForJobException(string message) : base(message) {}
        public AdaptorUserNotAuthorizedForJobException(string message, params object[] args) : base(message, args) {}
        public AdaptorUserNotAuthorizedForJobException(string message, Exception innerException) : base(message, innerException) {}
    }
}
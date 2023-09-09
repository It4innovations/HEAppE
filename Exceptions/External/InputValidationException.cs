using System;

namespace Exceptions.External
{
	public class InputValidationException : ExternalException {
		public InputValidationException(string message) : base(message) {}
        public InputValidationException(string message, params object[] args) : base(message, args) {}
        public InputValidationException(string message, Exception innerException) : base(message, innerException) {}
    }
}

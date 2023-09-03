using System;

namespace Exceptions.External
{
	public class InvalidRequestException : ExternalException
	{
		public InvalidRequestException(string message) : base(message) { }
        public InvalidRequestException(string message, params object[] args) : base(message, args) { }
    }
}
using System;

namespace Exceptions.External
{
    /// <summary>
    /// Represents external application exception
    /// </summary>
    public class ExternalException : Exception
    {
        public ExternalException(string message) : base(message) { }

        public ExternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}

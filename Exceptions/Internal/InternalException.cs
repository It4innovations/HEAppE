using System;

namespace Exceptions.Internal
{
    /// <summary>
    /// Represents intenal application exception
    /// </summary>
    public class InternalException : Exception
    {
        public InternalException(string message) : base(message) { }

        public InternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}

using System;

namespace HEAppE.Exceptions.AbstractTypes
{
    /// <summary>
    /// Represents internal application exception
    /// </summary>
    public class InternalException : BaseException
    {
        public InternalException(string message) : base(message) { }

        public InternalException(string message, params object[] args) : base(message, args) { }

        public InternalException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }

        public InternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}

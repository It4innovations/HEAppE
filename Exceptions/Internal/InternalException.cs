using Exceptions.Base;
using System;

namespace Exceptions.Internal
{
    /// <summary>
    /// Represents intenal application exception
    /// </summary>
    public class InternalException : BaseException
    {
        public InternalException(string message) : base(message) { }

        public InternalException(string message, params object[] args) : base(message, args) { }

        public InternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}

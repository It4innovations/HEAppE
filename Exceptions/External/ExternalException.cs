using Exceptions.Base;
using System;

namespace Exceptions.External
{
    /// <summary>
    /// Represents external application exception
    /// </summary>
    public class ExternalException : BaseException
    {
        public ExternalException(string message) : base(message) { }

        public ExternalException(string message, params object[] args) : base(message, args) { }

        public ExternalException(string message, Exception innerException) : base(message, innerException) { }
    }
}

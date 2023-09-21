using System;

namespace Exceptions.Base
{
    /// <summary>
    /// Represents base exception with args for localization message parameters
    /// </summary>
    public abstract class BaseException : Exception
    {
        public object[] Args { get; }

        public BaseException(string message) : base(message) { }

        public BaseException(string message, params object[] args) : base(message)
        {
            Args = args;
        }

        public BaseException(string message, Exception innerException, params object[] args) : base(message, innerException)
        {
            Args = args;
        }

        public BaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}

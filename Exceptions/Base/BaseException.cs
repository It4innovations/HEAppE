using System;

namespace Exceptions.Base
{
    /// <summary>
    /// Represents base exception with args for localization message parameters
    /// </summary>
    public class BaseException : Exception
    {
        public object[] Args { get; }

        public BaseException(string message) : base(message) { }

        public BaseException(string message, params object[] args) : base(message)
        {
            Args = args;
        }

        public BaseException(string message, Exception innerException) : base(message, innerException) { }
    }
}

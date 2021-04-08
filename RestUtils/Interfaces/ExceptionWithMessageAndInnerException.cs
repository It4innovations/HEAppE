using System;

namespace HEAppE.RestUtils.Interfaces
{
    public abstract class ExceptionWithMessageAndInnerException : Exception
    {
        public ExceptionWithMessageAndInnerException(string message) : base(message)
        {
        }
        public ExceptionWithMessageAndInnerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
using HEAppE.RestUtils.Interfaces;
using System;

namespace HEAppE.OpenStackAPI.Exceptions
{
    public class OpenStackAPIException : ExceptionWithMessageAndInnerException
    {
        public OpenStackAPIException(string message) : base(message)
        {
        }

        public OpenStackAPIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
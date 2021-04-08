using System;
using HEAppE.OpenStackAPI.JsonTypes;
using HEAppE.RestUtils.Interfaces;

namespace HEAppE.OpenStackAPI.Exceptions
{
    public class OpenStackAPIException : ExceptionWithMessageAndInnerException
    {
        public OpenStackAPIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
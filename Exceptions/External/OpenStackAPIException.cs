using HEAppE.RestUtils.Interfaces;
using System;

namespace Exceptions.External
{
    public class OpenStackAPIException : ExternalException
    {
        public OpenStackAPIException(string message) : base(message)
        {
        }

        public OpenStackAPIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

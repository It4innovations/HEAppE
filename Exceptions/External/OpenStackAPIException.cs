using System;

namespace Exceptions.External
{
    public class OpenStackAPIException : ExternalException
    {
        public OpenStackAPIException(string message) : base(message) { }

        public OpenStackAPIException(string message, Exception innerException) : base(message, innerException) { }

        public OpenStackAPIException(string message, params object[] args) : base(message, args) { }

        public OpenStackAPIException(string message, Exception innerException, params object[] args) : base(message, innerException, args) { }
    }
}

using HEAppE.Exceptions.AbstractTypes;
using System;

namespace HEAppE.Exceptions.External
{
    public class SshCAServiceTypeException : ExternalException
    {
        public SshCAServiceTypeException(string message) : base(message)
        {
        }

        public SshCAServiceTypeException(string message, params object[] args) : base(message, args)
        {
        }

        public SshCAServiceTypeException(string message, Exception innerException, params object[] args) : base(message,
            innerException, args)
        {
        }

        public SshCAServiceTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

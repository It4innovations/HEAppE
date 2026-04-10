using HEAppE.Exceptions.AbstractTypes;
using System;

namespace HEAppE.Exceptions.External
{
    public class SshCAServiceTypeException : ExternalException
    {
        public SshCAServiceTypeException(string message) : base(message)
        {
            ServiceName = "SshCaAPI";
        }

        public SshCAServiceTypeException(string message, params object[] args) : base(message, args)
        {
            ServiceName = "SshCaAPI";
        }

        public SshCAServiceTypeException(string message, Exception innerException, params object[] args) : base(message,
            innerException, args)
        {
            ServiceName = "SshCaAPI";
        }

        public SshCAServiceTypeException(string message, Exception innerException) : base(message, innerException)
        {
            ServiceName = "SshCaAPI";
        }
    }
}

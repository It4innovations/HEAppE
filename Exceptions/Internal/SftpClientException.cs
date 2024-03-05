using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal
{
    public class SftpClientException : InternalException
    {
        public SftpClientException(string message) : base(message) { }
        public SftpClientException(string message, params object[] args) : base(message, args) { }
        public SftpClientException(string message, Exception innerException) : base(message, innerException) { }
    }
}

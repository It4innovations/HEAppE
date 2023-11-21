using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal
{
    public class SftpClientArgumentException : InternalException
    {
        public SftpClientArgumentException(string message) : base(message) { }
        public SftpClientArgumentException(string message, params object[] args) : base(message, args) { }
        public SftpClientArgumentException(string message, Exception innerException) : base(message, innerException) { }
    }
}

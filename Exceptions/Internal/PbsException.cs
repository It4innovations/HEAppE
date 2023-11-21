using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal
{
    public class PbsException : InternalException
    {
        public PbsException(string message) : base(message) { }
        public PbsException(string message, params object[] args) : base(message, args) { }
        public PbsException(string message, Exception innerException) : base(message, innerException) { }
    }
}

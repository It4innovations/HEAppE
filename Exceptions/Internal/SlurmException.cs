using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal
{
    public class SlurmException : InternalException
    {
        public SlurmException(string message) : base(message) { }
        public SlurmException(string message, params object[] args) : base(message, args) { }
        public SlurmException(string message, Exception innerException) : base(message, innerException) { }
    }
}

using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class ResourceUsageException : ExternalException
    {
        public ResourceUsageException(string message) : base(message) { }
        public ResourceUsageException(string message, params object[] args) : base(message, args) { }
        public ResourceUsageException(string message, Exception innerException) : base(message, innerException) { }
    }
}

using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class CurrentUsageAndLimitationsException : ExternalException
    {
        public CurrentUsageAndLimitationsException(string message) : base(message) { }
        public CurrentUsageAndLimitationsException(string message, params object[] args) : base(message, args) { }
        public CurrentUsageAndLimitationsException(string message, Exception innerException) : base(message, innerException) { }
    }
}

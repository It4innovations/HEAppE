using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class RequestedJobResourcesExceededUserLimitationsException : ExternalException {
		public RequestedJobResourcesExceededUserLimitationsException(string message) : base(message) {}
        public RequestedJobResourcesExceededUserLimitationsException(string message, params object[] args) : base(message, args) {}
        public RequestedJobResourcesExceededUserLimitationsException(string message, Exception innerException) : base(message, innerException) {}
    }
}
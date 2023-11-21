using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External
{
    public class LexisAuthenticationException : ExternalException
    {
        public LexisAuthenticationException(string message) : base(message) {}

        public LexisAuthenticationException(string message, Exception innerException) : base(message, innerException) {}
    }
}
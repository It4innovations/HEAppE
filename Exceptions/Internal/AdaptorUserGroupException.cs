using HEAppE.Exceptions.AbstractTypes;
using System;

namespace HEAppE.Exceptions.Internal
{
    public class AdaptorUserGroupException : InternalException
    {
        public AdaptorUserGroupException(string message) : base(message) { }
        public AdaptorUserGroupException(string message, params object[] args) : base(message, args) { }
        public AdaptorUserGroupException(string message, Exception innerException) : base(message, innerException) { }
    }
}
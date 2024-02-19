using HEAppE.Exceptions.AbstractTypes;
using System;

namespace HEAppE.Exceptions.Internal
{
    public class DbContextException : InternalException
    {
        public DbContextException(string message) : base(message) { }
        public DbContextException(string message, params object[] args) : base(message, args) { }
        public DbContextException(string message, Exception innerException) : base(message, innerException) { }
    }
}

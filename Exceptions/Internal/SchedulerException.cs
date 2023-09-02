using System;

namespace Exceptions.Internal
{
    public class SchedulerException : InternalException
    {
        public SchedulerException(string message) : base(message) { }
        public SchedulerException(string message, params object[] args) : base(message, args) { }
        public SchedulerException(string message, Exception innerException) : base(message, innerException) { }
    }
}

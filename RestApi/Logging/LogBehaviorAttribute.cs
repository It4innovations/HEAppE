using System;

namespace HEAppE.RestApi.Logging
{
    public enum LoggingBehavior
    {
        /// <summary>
        /// Log everything including request/response bodies (default, in Debug/Trace mode).
        /// </summary>
        Full = 0,

        /// <summary>
        /// Log only high-level request (method, path) and response (status code).
        /// Bypass buffering and payload body logging.
        /// </summary>
        HeadersOnly = 1,

        /// <summary>
        /// Completely suppress request and response logging for this endpoint.
        /// </summary>
        None = 2
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class LogBehaviorAttribute : Attribute
    {
        public LoggingBehavior Behavior { get; }

        public LogBehaviorAttribute(LoggingBehavior behavior)
        {
            Behavior = behavior;
        }
    }
}

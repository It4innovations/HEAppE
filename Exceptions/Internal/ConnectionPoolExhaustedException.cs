using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal;

/// <summary>
/// Thrown when the SSH connection pool is saturated and a connection slot cannot be
/// acquired within the configured timeout. Callers (ExceptionMiddleware) should map
/// this to HTTP 429 Too Many Requests so clients can back off and retry.
/// </summary>
public class ConnectionPoolExhaustedException : InternalException
{
    public ConnectionPoolExhaustedException(string message) : base(message)
    {
    }

    public ConnectionPoolExhaustedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

using System;

namespace Services.Expirio.Exceptions;

public class ExpirioUpstreamException : ExpirioException
{
    public ExpirioUpstreamException(string message) : base(message)
    {
    }

    public ExpirioUpstreamException(string message, string details) : base(message, details)
    {
    }

    public ExpirioUpstreamException(string message, Exception innerException, string details) : base(message,
        innerException, details)
    {
    }

    public ExpirioUpstreamException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
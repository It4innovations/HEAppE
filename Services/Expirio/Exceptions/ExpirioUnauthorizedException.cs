using System;

namespace Services.Expirio.Exceptions;

public class ExpirioUnauthorizedException : ExpirioException
{
    public ExpirioUnauthorizedException(string message) : base(message)
    {
    }

    public ExpirioUnauthorizedException(string message, string details) : base(message, details)
    {
    }

    public ExpirioUnauthorizedException(string message, Exception innerException, string details) : base(message,
        innerException, details)
    {
    }

    public ExpirioUnauthorizedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
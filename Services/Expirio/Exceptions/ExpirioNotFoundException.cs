using System;

namespace Services.Expirio.Exceptions;

public class ExpirioNotFoundException : ExpirioException
{
    public ExpirioNotFoundException(string message) : base(message)
    {
    }

    public ExpirioNotFoundException(string message, string details) : base(message, details)
    {
    }

    public ExpirioNotFoundException(string message, Exception innerException, string details) : base(message,
        innerException, details)
    {
    }

    public ExpirioNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
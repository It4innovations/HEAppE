using System;

namespace Services.Expirio.Exceptions;

public class ExpirioServerException : ExpirioException
{
    public ExpirioServerException(string message) : base(message)
    {
    }

    public ExpirioServerException(string message, string details) : base(message, details)
    {
    }

    public ExpirioServerException(string message, Exception innerException, string details) : base(message,
        innerException, details)
    {
    }

    public ExpirioServerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
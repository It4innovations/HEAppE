using System;

namespace Services.Expirio.Exceptions;

public class ExpirioBadRequestException : ExpirioException
{
    public ExpirioBadRequestException(string message) : base(message)
    {
    }

    public ExpirioBadRequestException(string message, string details) : base(message, details)
    {
    }

    public ExpirioBadRequestException(string message, Exception innerException, string details) : base(message,
        innerException, details)
    {
    }

    public ExpirioBadRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
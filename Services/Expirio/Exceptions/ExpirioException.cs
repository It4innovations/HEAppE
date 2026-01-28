using System;

namespace Services.Expirio.Exceptions;

/// <summary>
///     Represents base Expirio exception with information details
/// </summary>
public class ExpirioException : Exception
{
    public ExpirioException(string message) : base(message)
    {
    }

    public ExpirioException(string message, string details) : base(message)
    {
        Details = details;
    }

    public ExpirioException(string message, Exception innerException, string details) : base(message, innerException)
    {
        Details = details;
    }

    public ExpirioException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public string Details { get; }
}
#pragma warning disable CS8618
using System;
using HEAppE.Exceptions.AbstractTypes;

namespace Services.Expirio.Exceptions;

/// <summary>
///     Represents base Expirio exception with information details
/// </summary>
public class ExpirioException : ExternalException
{
    public ExpirioException(string message) : base(message)
    {
        ServiceName = "Expirio";
    }

    public ExpirioException(string message, string details) : base(message)
    {
        Details = details;
        ServiceName = "Expirio";
    }

    public ExpirioException(string message, Exception innerException, string details) : base(message, innerException)
    {
        Details = details;
        ServiceName = "Expirio";
    }

    public ExpirioException(string message, Exception innerException) : base(message, innerException)
    {
        ServiceName = "Expirio";
    }
}
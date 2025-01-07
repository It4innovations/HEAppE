using System;

namespace HEAppE.Exceptions.AbstractTypes;

/// <summary>
///     Represents external application exception
/// </summary>
public class ExternalException : BaseException
{
    public ExternalException(string message) : base(message)
    {
    }

    public ExternalException(string message, params object[] args) : base(message, args)
    {
    }

    public ExternalException(string message, Exception innerException, params object[] args) : base(message,
        innerException, args)
    {
    }

    public ExternalException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
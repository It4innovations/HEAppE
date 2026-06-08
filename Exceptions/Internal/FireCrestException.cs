using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal;

public class FirecRestException : InternalException
{
    public FirecRestException(string message) : base(message)
    {
    }

    public FirecRestException(string message, params object[] args) : base(message, args)
    {
    }

    public FirecRestException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public required string CommandError { get; init; }
}
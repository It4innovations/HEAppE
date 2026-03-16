using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal;

public class FireCrestException : InternalException
{
    public FireCrestException(string message) : base(message)
    {
    }

    public FireCrestException(string message, params object[] args) : base(message, args)
    {
    }

    public FireCrestException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public required string CommandError { get; init; }
}
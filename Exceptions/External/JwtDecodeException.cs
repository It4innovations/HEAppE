using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

/// <summary>
///     Exception thrown when unable to parse JWT token.
/// </summary>
public class JwtDecodeException : ExternalException
{
    public JwtDecodeException(string message) : base(message)
    {
    }

    public JwtDecodeException(string message, params object[] args) : base(message, args)
    {
    }

    public JwtDecodeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
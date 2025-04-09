using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

public class SessionCodeNotValidException : ExternalException
{
    public SessionCodeNotValidException(string message) : base(message)
    {
    }

    public SessionCodeNotValidException(string message, params object[] args) : base(message, args)
    {
    }

    public SessionCodeNotValidException(string message, Exception innerException, params object[] args) : base(message,
        innerException, args)
    {
    }

    public SessionCodeNotValidException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
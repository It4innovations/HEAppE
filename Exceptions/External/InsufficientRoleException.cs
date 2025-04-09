using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

public class InsufficientRoleException : ExternalException
{
    public InsufficientRoleException(string message) : base(message)
    {
    }

    public InsufficientRoleException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public InsufficientRoleException(string message, params object[] args) : base(message, args)
    {
    }

    public InsufficientRoleException(string message, Exception innerException, params object[] args) : base(message,
        innerException, args)
    {
    }
}
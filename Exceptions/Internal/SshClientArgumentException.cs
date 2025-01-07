using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal;

public class SshClientArgumentException : InternalException
{
    public SshClientArgumentException(string message) : base(message)
    {
    }

    public SshClientArgumentException(string message, params object[] args) : base(message, args)
    {
    }

    public SshClientArgumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
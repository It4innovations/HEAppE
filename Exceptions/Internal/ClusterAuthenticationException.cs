using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.Internal;

public class ClusterAuthenticationException : InternalException
{
    public ClusterAuthenticationException(string message) : base(message)
    {
    }

    public ClusterAuthenticationException(string message, params object[] args) : base(message, args)
    {
    }

    public ClusterAuthenticationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
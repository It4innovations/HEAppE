using HEAppE.Exceptions.AbstractTypes;
using System;

namespace HEAppE.Exceptions.Internal;

public class DatabaseRestoreException : InternalException
{
    public DatabaseRestoreException(string message) : base(message)
    {
    }

    public DatabaseRestoreException(string message, params object[] args) : base(message, args)
    {
    }

    public DatabaseRestoreException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
using HEAppE.Exceptions.AbstractTypes;
using System;

namespace HEAppE.Exceptions.Internal;

public class DatabaseBackupException : InternalException
{
    public DatabaseBackupException(string message) : base(message)
    {
    }

    public DatabaseBackupException(string message, params object[] args) : base(message, args)
    {
    }

    public DatabaseBackupException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

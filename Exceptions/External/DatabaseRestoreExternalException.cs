using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

public class DatabaseRestoreExternalException : ExternalException
{
    public DatabaseRestoreExternalException(string message) : base(message)
    {
    }

    public DatabaseRestoreExternalException(string message, params object[] args) : base(message, args)
    {
    }
}

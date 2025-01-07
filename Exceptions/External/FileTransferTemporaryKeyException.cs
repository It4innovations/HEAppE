using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

public class FileTransferTemporaryKeyException : ExternalException
{
    public FileTransferTemporaryKeyException(string message) : base(message)
    {
    }
}
using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

public class SecureVaultException : ExternalException
{
    public SecureVaultException(string message) : base(message)
    {
    }

    public SecureVaultException(string message, params object[] args) : base(message, args)
    {
    }

    public SecureVaultException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
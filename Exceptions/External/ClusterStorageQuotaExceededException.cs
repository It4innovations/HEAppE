using System;
using HEAppE.Exceptions.AbstractTypes;

namespace HEAppE.Exceptions.External;

/// <summary>
///     Exception thrown when disk quota or storage space is exceeded on the HPC cluster.
/// </summary>
public class ClusterStorageQuotaExceededException : ExternalException
{
    public ClusterStorageQuotaExceededException(string message) : base(message)
    {
    }

    public ClusterStorageQuotaExceededException(string message, params object[] args) : base(message, args)
    {
    }

    public ClusterStorageQuotaExceededException(string message, Exception innerException, params object[] args) : base(message, innerException, args)
    {
    }
}

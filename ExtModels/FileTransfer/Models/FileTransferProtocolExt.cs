using System.ComponentModel;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// File transfer protocol types
/// </summary>
[Description("File transfer protocol types")]
public enum FileTransferProtocolExt
{
    NetworkShare = 1,
    SftpScp = 2,
    LocalSftpScp = 4,
    Http = 8,
    Https = 16
}
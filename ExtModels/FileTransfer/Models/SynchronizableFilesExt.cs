using System.ComponentModel;

namespace HEAppE.ExtModels.FileTransfer.Models;

/// <summary>
/// Synchronizable files types
/// </summary>
[Description("Synchronizable files types")]
public enum SynchronizableFilesExt
{
    LogFile = 0,
    ProgressFile = 1,
    StandardErrorFile = 2,
    StandardOutputFile = 3
}
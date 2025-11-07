using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Database backup type
/// </summary>
[Description("Database backup type")]
public enum DatabaseBackupTypeExt
{
    Full,
    Log
}

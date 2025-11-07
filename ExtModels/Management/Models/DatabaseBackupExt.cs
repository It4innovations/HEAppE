using System;
using System.ComponentModel;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
///     Database backup ext
/// </summary>
[Description("Database backup ext")]
public class DatabaseBackupExt
{
    /// <summary>
    ///     Type
    /// </summary>
    [Description("Type")]
    public DatabaseBackupTypeExt Type { get; set; }

    /// <summary>
    ///     File name
    /// </summary>
    [Description("Backup file name")]
    public string FileName { get; set; }

    /// <summary>
    ///     Timestamp
    /// </summary>
    [Description("Timestamp")]
    public DateTime TimeStamp { get; set; }

    /// <summary>
    ///     File size in MB
    /// </summary>
    [Description("File size in MB")]
    public decimal FileSizeMB { get; set; }

    /// <summary>
    ///     Path
    /// </summary>
    [Description("Path")]
    public string Path { get; set; }
}

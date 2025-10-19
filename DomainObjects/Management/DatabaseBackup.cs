using System;

namespace HEAppE.DomainObjects.Management;

public class DatabaseBackup
{
    public DatabaseBackupType Type { get; set; }
    public string FileName { get; set; }
    public DateTime TimeStamp { get; set; }
    public decimal FileSizeMB { get; set; }
    public string Path { get; set; }
}

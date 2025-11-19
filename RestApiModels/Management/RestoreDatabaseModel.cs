using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List database backups model
/// </summary>
[DataContract(Name = "RestoreDatabaseModel")]
[Description("Restore database model")]
public class RestoreDatabaseModel : SessionCodeModel
{
    /// <summary>
    /// Backup file name
    /// </summary>
    [DataMember(Name = "BackupFileName", IsRequired = true)]
    [Description("Backup file name")]
    public string BackupFileName { get; set; }

    /// <summary>
    /// Include transaction logs backup restore
    /// </summary>
    [DataMember(Name = "BackupFile", IsRequired = false)]
    [Description("Include transaction logs backup restore")]
    public bool IncludeLogs { get; set; }
}

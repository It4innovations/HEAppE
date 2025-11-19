using HEAppE.ExtModels.Management.Models;
using HEAppE.RestApiModels.AbstractModels;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// List database backups model
/// </summary>
[DataContract(Name = "ListDatabaseBackupsModel")]
[Description("List database backups model")]
public class ListDatabaseBackupsModel : SessionCodeModel
{
    /// <summary>
    /// From date time
    /// </summary>
    [DataMember(Name = "FromDateTime", IsRequired = false)]
    [Description("From date time")]
    public DateTime? FromDateTime { get; set; }

    /// <summary>
    /// To date time
    /// </summary>
    [DataMember(Name = "ValidityTo", IsRequired = false)]
    [Description("To date time")]
    public DateTime? ToDateTime { get; set; }

    /// <summary>
    /// Database log type
    /// </summary>
    [DataMember(Name = "DatabaseLogType", IsRequired = false)]
    [Description("Database log type")]
    public DatabaseBackupTypeExt? Type { get; set; }
}

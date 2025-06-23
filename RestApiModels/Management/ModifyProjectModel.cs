using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify project model
/// </summary>
[DataContract(Name = "ModifyProjectModel")]
[Description("Modify project model")]
public class ModifyProjectModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = false)]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    [DataMember(Name = "StartDate", IsRequired = true)]
    [Description("Start date")]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    [DataMember(Name = "EndDate", IsRequired = true)]
    [Description("End date")]
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Usage type
    /// </summary>
    [DataMember(Name = "UsageType", IsRequired = true)]
    [Description("Usage type")]
    public UsageTypeExt? UsageType { get; set; }

    /// <summary>
    /// Use accounting string for scheduler
    /// </summary>
    [DataMember(Name = "UseAccountingStringForScheduler", IsRequired = false)]
    [Description("Use accounting string for scheduler")]
    public bool? UseAccountingStringForScheduler { get; set; }

    /// <summary>
    /// Map user account to exact robot account
    /// </summary>
    [DataMember(Name = "IsOneToOneMapping", IsRequired = false)]
    [Description("Map user account to exact robot account")]
    public bool? IsOneToOneMapping { get; set; } = false;    
}
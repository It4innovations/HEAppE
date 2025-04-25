using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create project model
/// </summary>
[DataContract(Name = "CreateProjectModel")]
[Description("Create project model")]
public class CreateProjectModel : SessionCodeModel
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
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
    /// Accounting string
    /// </summary>
    [DataMember(Name = "AccountingString", IsRequired = true)]
    [StringLength(20)]
    [Description("Accounting string")]
    public string AccountingString { get; set; }

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
    public bool UseAccountingStringForScheduler { get; set; } = true;

    /// <summary>
    /// PIEmail
    /// </summary>
    [DataMember(Name = "PIEmail", IsRequired = true)]
    [StringLength(255)]
    [Description("PIEmail")]
    public string PIEmail { get; set; }
}
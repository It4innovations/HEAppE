using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateProjectModel")]
public class CreateProjectModel : SessionCodeModel
{
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(50)]
    public string Name { get; set; }

    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    public string Description { get; set; }

    [DataMember(Name = "AccountingString", IsRequired = true)]
    [StringLength(20)]
    public string AccountingString { get; set; }

    [DataMember(Name = "StartDate", IsRequired = true)]
    public DateTime StartDate { get; set; }

    [DataMember(Name = "EndDate", IsRequired = true)]
    public DateTime EndDate { get; set; }

    [DataMember(Name = "UsageType", IsRequired = true)]
    public UsageTypeExt? UsageType { get; set; }

    [DataMember(Name = "UseAccountingStringForScheduler", IsRequired = false)]
    public bool UseAccountingStringForScheduler { get; set; } = true;

    [DataMember(Name = "PIEmail", IsRequired = true)]
    [StringLength(255)]
    public string PIEmail { get; set; }
}
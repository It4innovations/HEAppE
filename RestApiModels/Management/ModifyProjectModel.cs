using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobReporting.Models;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifyProjectModel")]
public class ModifyProjectModel : SessionCodeModel
{
    [DataMember(Name = "Id", IsRequired = true)]
    public long Id { get; set; }

    [DataMember(Name = "Name", IsRequired = false)]
    [StringLength(50)]
    public string Name { get; set; }

    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(100)]
    public string Description { get; set; }

    [DataMember(Name = "StartDate", IsRequired = true)]
    public DateTime StartDate { get; set; }

    [DataMember(Name = "EndDate", IsRequired = true)]
    public DateTime EndDate { get; set; }

    [DataMember(Name = "UsageType", IsRequired = true)]
    public UsageTypeExt? UsageType { get; set; }

    [DataMember(Name = "UseAccountingStringForScheduler", IsRequired = false)]
    public bool? UseAccountingStringForScheduler { get; set; }
}
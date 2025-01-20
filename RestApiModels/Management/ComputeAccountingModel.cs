using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Compute accounting model
/// </summary>
[DataContract(Name = "ComputeAccountingModel")]
[Description("Compute accounting model")]
public class ComputeAccountingModel : SessionCodeModel
{
    /// <summary>
    /// Start time
    /// </summary>
    [DataMember(Name = "StartTime", IsRequired = true)]
    [Description("Start time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [DataMember(Name = "EndTime", IsRequired = true)]
    [Description("End time")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }
}
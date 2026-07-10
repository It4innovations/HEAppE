using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for opening a QScheduler session explicitly
/// </summary>
[DataContract(Name = "OpenQSchedulerSessionModel")]
[Description("Model for opening a QScheduler session explicitly")]
public class OpenQSchedulerSessionModel : SessionCodeModel
{
    /// <summary>
    /// Cluster ID
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster ID")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Project ID
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project ID")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Target Machine ID
    /// </summary>
    [DataMember(Name = "MachineId")]
    [Description("Target Machine ID")]
    public string MachineId { get; set; }

    /// <summary>
    /// Walltime limit for the session in seconds
    /// </summary>
    [DataMember(Name = "WalltimeLimit")]
    [Description("Walltime limit for the session in seconds")]
    public int WalltimeLimit { get; set; }
}

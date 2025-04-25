using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models;

/// <summary>
/// Scheduler type ext
/// </summary>
[DataContract(Name = "SchedulerTypeExt")]
[Description("SchedulerType ext")]
public enum SchedulerTypeExt
{
    LinuxLocal = 1,
    PbsPro = 2,
    Slurm = 4,
    HyperQueue = 8
}
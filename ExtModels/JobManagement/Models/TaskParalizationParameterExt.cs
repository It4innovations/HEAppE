using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Task paralization parameter ext
/// </summary>
[DataContract(Name = "TaskParalizationParameterExt")]
[Description("Task paralization parameter ext")]
public class TaskParalizationParameterExt
{
    /// <summary>
    /// MPI processes
    /// </summary>
    [DataMember(Name = "MPIProcesses")]
    [Description("MPI processes")]
    public int? MPIProcesses { get; set; }

    /// <summary>
    /// Open MP threads
    /// </summary>
    [DataMember(Name = "OpenMPThreads")]
    [Description("Open MP threads")]
    public int? OpenMPThreads { get; set; }

    /// <summary>
    /// Maximum cores
    /// </summary>
    [DataMember(Name = "MaxCores")]
    [Description("Maximum cores")]
    public int MaxCores { get; set; }

    public override string ToString()
    {
        return $"TaskParalizationParameterExt(MPIProcesses={MPIProcesses}; OpenMPThreads={OpenMPThreads}; MaxCores={MaxCores})";
    }
}
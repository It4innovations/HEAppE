using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Submitted job info ext
/// </summary>
[DataContract(Name = "SubmittedJobInfoExt")]
[Description("Submitted job info ext")]
public class SubmittedJobInfoExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long? Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// State
    /// </summary>
    [DataMember(Name = "State")]
    [Description("State")]
    public JobStateExt? State { get; set; }

    /// <summary>
    /// Creation time
    /// </summary>
    [DataMember(Name = "CreationTime")]
    [Description("Creation time")]
    public DateTime? CreationTime { get; set; }

    /// <summary>
    /// Submit time
    /// </summary>
    [DataMember(Name = "SubmitTime")]
    [Description("Submit time")]
    public DateTime? SubmitTime { get; set; }

    /// <summary>
    /// Start time
    /// </summary>
    [DataMember(Name = "StartTime")]
    [Description("Start time")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [DataMember(Name = "EndTime")]
    [Description("End time")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total allocation time
    /// </summary>
    [DataMember(Name = "TotalAllocatedTime")]
    [Description("Total allocation time")]
    public double? TotalAllocatedTime { get; set; }

    /// <summary>
    /// Sub project
    /// </summary>
    [DataMember(Name = "SubProject")]
    [Description("Sub project")]
    public string SubProject { get; set; }

    /// <summary>
    /// Array of tasks
    /// </summary>
    [DataMember(Name = "Tasks")]
    [Description("Array of tasks")]
    public SubmittedTaskInfoExt[] Tasks { get; set; }

    public override string ToString()
    {
        return $"SubmittedJobInfoExt(id={Id}; name={Name}; state={State}; creationTime={CreationTime}; submitTime={SubmitTime}; startTime={StartTime}; endTime={EndTime}; totalAllocatedTime={TotalAllocatedTime}; subProject={SubProject}; tasks={Tasks})";
    }
}
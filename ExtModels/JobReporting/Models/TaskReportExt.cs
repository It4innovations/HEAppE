using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

[DataContract(Name = "TaskReportExt")]
public class TaskReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public double? Usage { get; set; }
}
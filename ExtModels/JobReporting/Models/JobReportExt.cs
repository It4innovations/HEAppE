using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

[DataContract(Name = "JobReportExt")]
public class JobReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public string SubProject { get; set; }

    [DataMember] public List<TaskReportExt> Tasks { get; set; }
}
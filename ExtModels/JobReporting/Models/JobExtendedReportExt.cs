using System.Collections.Generic;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.JobReporting.Models;

[DataContract(Name = "JobExtendedReportExt")]
public class JobExtendedReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public JobStateExt State { get; set; }

    [DataMember] public List<TaskExtendedReportExt> Tasks { get; set; }
}
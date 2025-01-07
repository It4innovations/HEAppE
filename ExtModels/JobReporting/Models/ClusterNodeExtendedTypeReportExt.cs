using System.Collections.Generic;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobReporting.Models;

[DataContract(Name = "ClusterNodeExtendedTypeReportExt")]
public class ClusterNodeExtendedTypeReportExt
{
    [DataMember] public long Id { get; set; }

    [DataMember] public double? TotalUsage { get; set; }

    [DataMember] public string Name { get; set; }

    [DataMember] public string Description { get; set; }

    [DataMember] public List<JobExtendedReportExt> Jobs { get; set; }
}
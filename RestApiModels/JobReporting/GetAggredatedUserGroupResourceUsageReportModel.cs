using System;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

[DataContract(Name = "GetAggredatedUserGroupResourceUsageReportModel")]
public class GetAggredatedUserGroupResourceUsageReportModel : SessionCodeModel
{
    [DataMember(Name = "StartTime")] public DateTime StartTime { get; set; }

    [DataMember(Name = "EndTime")] public DateTime EndTime { get; set; }

    public override string ToString()
    {
        return
            $"GetAggredatedUserGroupResourceUsageReportModel({base.ToString()}; StartTime: {StartTime}; EndTime: {EndTime})";
    }
}
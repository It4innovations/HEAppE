using System;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

[DataContract(Name = "UserResourceUsageReportModel")]
public class UserResourceUsageReportModel : SessionCodeModel
{
    [DataMember(Name = "UserId")] public long UserId { get; set; }

    [DataMember(Name = "StartTime")] public DateTime StartTime { get; set; }

    [DataMember(Name = "EndTime")] public DateTime EndTime { get; set; }

    public override string ToString()
    {
        return
            $"UserResourceUsageReportModel({base.ToString()}; UserId: {UserId}; StartTime: {StartTime}; EndTime: {EndTime})";
    }
}
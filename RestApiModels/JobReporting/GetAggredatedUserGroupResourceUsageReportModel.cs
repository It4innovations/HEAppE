using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

/// <summary>
/// Model for retrieving usage report for aggredated user group
/// </summary>
[DataContract(Name = "GetAggredatedUserGroupResourceUsageReportModel")]
[Description("Model for retrieving usage report for aggredated user group")]
public class GetAggredatedUserGroupResourceUsageReportModel : SessionCodeModel
{
    /// <summary>
    /// Start time
    /// </summary>
    [DataMember(Name = "StartTime")]
    [Description("Start time")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// End time
    /// </summary>
    [DataMember(Name = "EndTime")]
    [Description("End time")]
    public DateTime EndTime { get; set; }

    public override string ToString()
    {
        return
            $"GetAggredatedUserGroupResourceUsageReportModel({base.ToString()}; StartTime: {StartTime}; EndTime: {EndTime})";
    }
}
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

/// <summary>
/// Model for retrieving usage report for user group
/// </summary>
[DataContract(Name = "UserGroupResourceUsageReportModel")]
[Description("Model for retrieving usage report for user group")]
public class UserGroupResourceUsageReportModel : SessionCodeModel
{
    /// <summary>
    /// Group id
    /// </summary>
    [DataMember(Name = "GroupId")]
    [Description("Group id")]
    public long GroupId { get; set; }

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
            $"UserGroupResourceUsageReportModel({base.ToString()}; GroupId: {GroupId}; StartTime: {StartTime}; EndTime: {EndTime})";
    }
}
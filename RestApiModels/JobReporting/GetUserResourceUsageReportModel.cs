using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

/// <summary>
/// Model for retrieving usage report for user
/// </summary>
[DataContract(Name = "UserResourceUsageReportModel")]
[Description("Model for retrieving usage report for user")]
public class UserResourceUsageReportModel : SessionCodeModel
{
    /// <summary>
    /// User id
    /// </summary>
    [DataMember(Name = "UserId")]
    [Description("User id")]
    public long UserId { get; set; }

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
            $"UserResourceUsageReportModel({base.ToString()}; UserId: {UserId}; StartTime: {StartTime}; EndTime: {EndTime})";
    }
}
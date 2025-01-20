using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobReporting;

/// <summary>
/// Model for retrieving usage report for job
/// </summary>
[DataContract(Name = "ResourceUsageReportForJobModel")]
[Description("Model for retrieving usage report for job")]
public class ResourceUsageReportForJobModel : SessionCodeModel
{
    /// <summary>
    /// Job id
    /// </summary>
    [DataMember(Name = "JobId")]
    [Description("Job id")]
    public long JobId { get; set; }

    public override string ToString()
    {
        return $"ResourceUsageReportForJobModel({base.ToString()}; JobId: {JobId})";
    }
}
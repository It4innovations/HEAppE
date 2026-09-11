using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Request model for retrieving external service and database telemetry logs for a specific job.
/// </summary>
[DataContract(Name = "GetJobExternalServiceLogsModel")]
[Description("Job external services telemetry request")]
public class GetJobExternalServiceLogsModel : SessionCodeModel
{
    /// <summary>
    /// Identifier of the submitted job.
    /// </summary>
    [Required]
    [DataMember(Name = "SubmittedJobInfoId")]
    [Description("Submitted job info identifier")]
    public long SubmittedJobInfoId { get; set; }

    public override string ToString()
        => $"GetJobExternalServiceLogsModel({base.ToString()}; SubmittedJobInfoId: {SubmittedJobInfoId})";
}

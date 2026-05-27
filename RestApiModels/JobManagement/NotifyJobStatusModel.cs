using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for notifying job status callback
/// </summary>
[DataContract(Name = "NotifyJobStatusModel")]
[Description("Model for notifying job status callback")]
public class NotifyJobStatusModel
{
    /// <summary>
    /// The scheduler native job ID
    /// </summary>
    [DataMember]
    [Required]
    public string SchedulerJobId { get; set; }

    /// <summary>
    /// The native status payload from the scheduler
    /// </summary>
    [DataMember]
    [Required]
    public string Payload { get; set; }

    public override string ToString()
    {
        return $"NotifyJobStatusModel(SchedulerJobId={SchedulerJobId})";
    }
}

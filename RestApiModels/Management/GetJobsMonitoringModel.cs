using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Request model for GET /heappe/Management/GetJobsMonitoring.
/// Uses keyset pagination: pass the <c>LastJobId</c> returned in the previous response
/// as the cursor for the next page.
/// </summary>
[DataContract(Name = "GetJobsMonitoringModel")]
[Description("Admin job monitoring request with keyset pagination")]
public class GetJobsMonitoringModel : SessionCodeModel
{
    /// <summary>
    /// Number of jobs to return per page. Must be between 1 and 200. Defaults to 50.
    /// </summary>
    [DataMember(Name = "PageSize")]
    [Description("Number of jobs per page (1–200)")]
    [Range(1, 200)]
    public int PageSize { get; set; } = 50;

    /// <summary>
    /// Keyset cursor: return only jobs with Id strictly less than this value.
    /// Omit (or set to null) to start from the most recent job.
    /// </summary>
    [DataMember(Name = "LastJobId")]
    [Description("Cursor for keyset pagination – Id from the previous page's NextCursorId")]
    public long? LastJobId { get; set; }

    public override string ToString()
        => $"GetJobsMonitoringModel({base.ToString()}; PageSize: {PageSize}; LastJobId: {LastJobId})";
}

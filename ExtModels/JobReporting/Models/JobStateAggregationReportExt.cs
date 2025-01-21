using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.JobReporting.Models;

/// <summary>
/// Job state aggregation report ext
/// </summary>
[DataContract(Name = "JobStateAggregationReportExt")]
[Description("Job state aggregation report ext")]
public class JobStateAggregationReportExt
{
    /// <summary>
    /// Job state id
    /// </summary>
    [DataMember(Name = "JobStateId")]
    [Description("Job state id")]
    public JobStateExt JobStateId { get; set; }

    /// <summary>
    /// Job state name
    /// </summary>
    [DataMember(Name = "JobStateName")]
    [Description("Job state name")]
    public string JobStateName { get; set; }

    /// <summary>
    /// Count
    /// </summary>
    [DataMember(Name = "Count")]
    [Description("Count")]
    public long Count { get; set; }

    public override string ToString()
    {
        return $"JobStateAggregationReportExt: State={JobStateId}, StateName={JobStateName}, Count={Count}";
    }
}
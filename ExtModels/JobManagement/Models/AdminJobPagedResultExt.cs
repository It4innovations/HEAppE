using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Paged result container for admin job listing
/// </summary>
[DataContract(Name = "AdminJobPagedResultExt")]
[Description("Paged result container for admin job listing")]
public class AdminJobPagedResultExt
{
    /// <summary>
    /// Total count of matching jobs for pagination calculation
    /// </summary>
    [DataMember(Name = "TotalCount")]
    [Description("Total count of matching jobs for pagination calculation")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Max items requested per page
    /// </summary>
    [DataMember(Name = "Limit")]
    [Description("Max items requested per page")]
    public int? Limit { get; set; }

    /// <summary>
    /// Items skipped for pagination
    /// </summary>
    [DataMember(Name = "Offset")]
    [Description("Items skipped for pagination")]
    public int? Offset { get; set; }

    /// <summary>
    /// Array of enriched jobs
    /// </summary>
    [DataMember(Name = "Items")]
    [Description("Array of enriched jobs")]
    public AdminSubmittedJobInfoExt[] Items { get; set; }

    public override string ToString()
    {
        return $"AdminJobPagedResultExt(totalCount={TotalCount}; limit={Limit}; offset={Offset}; itemsCount={Items?.Length})";
    }
}

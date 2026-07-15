using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Request model for GET /heappe/Management/GetExternalServicesReport.
/// Triggers live health probes for all registered external services and
/// returns historical telemetry statistics for the requested time window.
/// </summary>
[DataContract(Name = "GetExternalServicesReportModel")]
[Description("Admin external services report request")]
public class GetExternalServicesReportModel : SessionCodeModel
{
    /// <summary>
    /// Start of the time window for historical statistics (UTC).
    /// Defaults to 24 hours before the request is received.
    /// </summary>
    [DataMember(Name = "From")]
    [Description("Start of the statistics time window (UTC)")]
    public DateTime? From { get; set; }

    /// <summary>
    /// End of the time window for historical statistics (UTC).
    /// Defaults to the moment the request is received.
    /// </summary>
    [DataMember(Name = "To")]
    [Description("End of the statistics time window (UTC)")]
    public DateTime? To { get; set; }

    public override string ToString()
        => $"GetExternalServicesReportModel({base.ToString()}; From: {From}; To: {To})";
}

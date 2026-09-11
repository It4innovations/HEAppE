using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Request model for querying aggregated external services telemetry statistics.
/// </summary>
[DataContract(Name = "GetExternalServicesStatisticsModel")]
[Description("External services telemetry statistics request")]
public class GetExternalServicesStatisticsModel : SessionCodeModel
{
    /// <summary>
    /// Start of the time window for statistics (UTC). Defaults to 24 hours ago.
    /// </summary>
    [DataMember(Name = "From")]
    [Description("Start of the statistics time window (UTC)")]
    public DateTime? From { get; set; }

    /// <summary>
    /// End of the time window for statistics (UTC). Defaults to current time.
    /// </summary>
    [DataMember(Name = "To")]
    [Description("End of the statistics time window (UTC)")]
    public DateTime? To { get; set; }

    /// <summary>
    /// Optional filter by specific service name (e.g. "hashicorp vault", "keycloak", "database", cluster name).
    /// </summary>
    [DataMember(Name = "ServiceName")]
    [Description("Optional filter by service name")]
    public string ServiceName { get; set; }

    /// <summary>
    /// Optional filter by specific cluster ID.
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Optional filter by cluster ID")]
    public long? ClusterId { get; set; }

    public override string ToString()
        => $"GetExternalServicesStatisticsModel({base.ToString()}; From: {From}; To: {To}; ServiceName: {ServiceName}; ClusterId: {ClusterId})";
}

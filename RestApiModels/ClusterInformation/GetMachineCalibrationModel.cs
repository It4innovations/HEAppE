using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

/// <summary>
/// Model for retrieving QScheduler machine calibration
/// </summary>
[DataContract(Name = "GetMachineCalibrationModel")]
[Description("Model for retrieving QScheduler machine calibration")]
public class GetMachineCalibrationModel : SessionCodeModel
{
    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId")]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Calibration set identifier, or 'default'
    /// </summary>
    [DataMember(Name = "CalibrationId")]
    [Description("Calibration set identifier, or 'default'")]
    public string CalibrationId { get; set; }

    /// <summary>
    /// Optional calibration endpoint (e.g. 'readout', 'gates'). When omitted, the whole calibration set is returned.
    /// </summary>
    [DataMember(Name = "Endpoint", IsRequired = false)]
    [Description("Optional calibration endpoint. When omitted, the whole calibration set is returned.")]
    public string Endpoint { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"GetMachineCalibrationModel({base.ToString()}; ClusterNodeTypeId: {ClusterNodeTypeId}; CalibrationId: {CalibrationId}; Endpoint: {Endpoint}; ProjectId: {ProjectId})";
    }
}

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
    /// Cluster id
    /// </summary>
    [DataMember(Name = "ClusterId")]
    [Description("Cluster id")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Machine id in QScheduler
    /// </summary>
    [DataMember(Name = "MachineId")]
    [Description("Machine id in QScheduler")]
    public string MachineId { get; set; }

    /// <summary>
    /// Calibration identifier
    /// </summary>
    [DataMember(Name = "CalibrationId")]
    [Description("Calibration identifier")]
    public string CalibrationId { get; set; }

    /// <summary>
    /// Calibration endpoint (e.g. 'readout', 'gates')
    /// </summary>
    [DataMember(Name = "Endpoint")]
    [Description("Calibration endpoint")]
    public string Endpoint { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"GetMachineCalibrationModel({base.ToString()}; ClusterId: {ClusterId}; MachineId: {MachineId}; CalibrationId: {CalibrationId}; Endpoint: {Endpoint}; ProjectId: {ProjectId})";
    }
}

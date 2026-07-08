using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

/// <summary>
/// Model for retrieving QScheduler machine architecture
/// </summary>
[DataContract(Name = "GetMachineArchitectureModel")]
[Description("Model for retrieving QScheduler machine architecture")]
public class GetMachineArchitectureModel : SessionCodeModel
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
    public int MachineId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"GetMachineArchitectureModel({base.ToString()}; ClusterId: {ClusterId}; MachineId: {MachineId}; ProjectId: {ProjectId})";
    }
}

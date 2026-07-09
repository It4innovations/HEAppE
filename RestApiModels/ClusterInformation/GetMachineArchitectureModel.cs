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
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId")]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"GetMachineArchitectureModel({base.ToString()}; ClusterNodeTypeId: {ClusterNodeTypeId}; ProjectId: {ProjectId})";
    }
}

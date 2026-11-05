using System.ComponentModel;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.ClusterInformation;

/// <summary>
/// Model for retrieving a QScheduler machine artifact
/// </summary>
[DataContract(Name = "GetMachineArtifactModel")]
[Description("Model for retrieving a QScheduler machine artifact")]
public class GetMachineArtifactModel : SessionCodeModel
{
    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId")]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Artifact name (settings, chip-design-records, channel-properties, static-quantum-architectures, duts)
    /// </summary>
    [DataMember(Name = "ArtifactName")]
    [Description("Artifact name (settings, chip-design-records, channel-properties, static-quantum-architectures, duts)")]
    public string ArtifactName { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    public override string ToString()
    {
        return $"GetMachineArtifactModel({base.ToString()}; ClusterNodeTypeId: {ClusterNodeTypeId}; ArtifactName: {ArtifactName}; ProjectId: {ProjectId})";
    }
}

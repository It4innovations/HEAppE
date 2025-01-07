using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "RemoveProjectAssignmentToClusterModel")]
public class RemoveProjectAssignmentToClusterModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    [DataMember(Name = "ClusterId", IsRequired = true)]
    public long ClusterId { get; set; }
}
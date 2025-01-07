using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

[DataContract(Name = "ClusterExt")]
public class ClusterExt
{
    [DataMember(Name = "Id")] public long? Id { get; set; }

    [DataMember(Name = "Name")] public string Name { get; set; }

    [DataMember(Name = "Description")] public string Description { get; set; }

    [DataMember(Name = "NodeTypes")] public ClusterNodeTypeExt[] NodeTypes { get; set; }

    public override string ToString()
    {
        return $"ClusterInfoExt(Id={Id}; Name={Name}; Description={Description}; NodeTypes={NodeTypes})";
    }
}
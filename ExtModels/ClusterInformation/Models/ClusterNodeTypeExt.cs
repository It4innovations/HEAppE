using System.Runtime.Serialization;
using HEAppE.ExtModels.JobManagement.Models;

namespace HEAppE.ExtModels.ClusterInformation.Models;

[DataContract(Name = "ClusterNodeTypeExt")]
public class ClusterNodeTypeExt
{
    [DataMember(Name = "Id")] public long? Id { get; set; }

    [DataMember(Name = "Name")] public string Name { get; set; }

    [DataMember(Name = "Description")] public string Description { get; set; }

    [DataMember(Name = "NumberOfNodes")] public int? NumberOfNodes { get; set; }

    [DataMember(Name = "CoresPerNode")] public int? CoresPerNode { get; set; }

    [DataMember(Name = "MaxWalltime")] public int? MaxWalltime { get; set; }

    [DataMember(Name = "FileTransferMethodId")]
    public long? FileTransferMethodId { get; set; }

    [DataMember(Name = "Projects")] public ProjectExt[] Projects { get; set; }

    public override string ToString()
    {
        return
            $"ClusterNodeTypeExt(id={Id}; name={Name}; description={Description}; numberOfNodes={NumberOfNodes}; coresPerNode={CoresPerNode}; maxWalltime={MaxWalltime}; fileTransferMethodId={FileTransferMethodId}; projects={Projects})";
    }
}
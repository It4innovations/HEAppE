using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

[DataContract(Name = "ClusterProjectExt")]
public class ClusterProjectExt
{
    [DataMember(Name = "Id")] public long ClusterId { get; set; }

    [DataMember(Name = "ProjectId")] public long ProjectId { get; set; }

    [DataMember(Name = "LocalBasepath")]
    [StringLength(100)]
    public string LocalBasepath { get; set; }

    [DataMember(Name = "CreatedAt")] public DateTime CreatedAt { get; set; }

    [DataMember(Name = "ModifiedAt")] public DateTime? ModifiedAt { get; set; }

    public override string ToString()
    {
        return
            $"""ClusterProjectExt: ClusterId={ClusterId}, ProjectId={ProjectId}, LocalBasepath={LocalBasepath}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}" """;
    }
}
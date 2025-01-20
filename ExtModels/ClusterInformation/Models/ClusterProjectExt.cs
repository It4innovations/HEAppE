using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models;

/// <summary>
/// Cluster project ext
/// </summary>
[DataContract(Name = "ClusterProjectExt")]
[Description("Cluster project ext")]
public class ClusterProjectExt
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id")]
    [Description("Id")]
    public long ClusterId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId")]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Local base path
    /// </summary>
    [DataMember(Name = "LocalBasepath")]
    [StringLength(100)]
    [Description("Local base path")]
    public string LocalBasepath { get; set; }

    /// <summary>
    /// Created at
    /// </summary>
    [DataMember(Name = "CreatedAt")]
    [Description("Created at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Modified at
    /// </summary>
    [DataMember(Name = "ModifiedAt")]
    [Description("Modified at")]
    public DateTime? ModifiedAt { get; set; }

    public override string ToString()
    {
        return $"""ClusterProjectExt: ClusterId={ClusterId}, ProjectId={ProjectId}, LocalBasepath={LocalBasepath}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}" """;
    }
}
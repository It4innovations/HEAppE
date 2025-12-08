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
    /// Scratch Storage Path
    /// </summary>
    [DataMember(Name = "ScratchStoragePath")]
    [StringLength(100)]
    [Description("Scratch Storage Path")]
    public string ScratchStoragePath { get; set; }
    
    /// <summary>
    /// Project Storage Path
    /// </summary>
    [DataMember(Name = "ProjectStoragePath")]
    [StringLength(100)]
    [Description("Project Storage Path")]
    public string ProjectStoragePath { get; set; }

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
        return $"""ClusterProjectExt: ClusterId={ClusterId}, ProjectId={ProjectId}, ScratchStoragePath={ScratchStoragePath}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}" """;
    }
}
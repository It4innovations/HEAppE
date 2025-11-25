using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.JobManagement;

/// <summary>
/// Model for dry run job
/// </summary>
[DataContract(Name = "DryRunJobModel")]
[Description("Dry Run Job Model")]
public class DryRunJobModel : SessionCodeModel
{
    [DataMember(Name = "ProjectId")]
    [Description("ProjectId")]
    [Required]
    public long ProjectId { get; set; }
    
    [DataMember(Name = "ClusterNodeTypeId")]
    [Description("Cluster Node Type Id")]
    [Required]
    public long ClusterNodeTypeId { get; set;  }
    
    [DataMember(Name = "Nodes")]
    [Description("Number of nodes")]
    [Required]
    public long Nodes { get; set;  }
    
    [DataMember(Name = "TasksPerNode")]
    [Description("Number of tasks per node")]
    [Required]
    public long TasksPerNode { get; set;  }
    
    [DataMember(Name = "WallTimeInMinutes")]
    [Description("Wall time in minutes")]
    [Required]
    public long WallTimeInMinutes { get; set;  }
}
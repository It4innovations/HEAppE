using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobManagement;

[Table("ClusterProject")]
public class ClusterProject : IdentifiableDbEntity //, ISoftDeletableEntity
{
    public long ClusterId { get; set; }
    public virtual Cluster Cluster { get; set; }

    public long ProjectId { get; set; }
    public virtual Project Project { get; set; }

    [Required] [StringLength(100)] public string ScratchStoragePath { get; set; }
    [StringLength(100)] public string ProjectStoragePath { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    [Required] public bool IsDeleted { get; set; } = false;

    public virtual List<ClusterProjectCredential> ClusterProjectCredentials { get; set; } = new();

    public override string ToString()
    {
        return
            $"""ClusterProject: Cluster={Cluster}, Project={Project}, ScratchStoragePath={ScratchStoragePath}, ProjectStoragePath={ProjectStoragePath}; CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}" """;
    }
}
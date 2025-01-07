using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.DomainObjects.UserAndLimitationManagement;

namespace HEAppE.DomainObjects.JobManagement;

[Table("Project")]
public class Project : IdentifiableDbEntity, ISoftDeletableEntity
{
    [Required] [StringLength(50)] public string Name { get; set; }

    [StringLength(100)] public string Description { get; set; }

    [Required] [StringLength(20)] public string AccountingString { get; set; }

    [Required] public DateTime StartDate { get; set; }

    [Required] public DateTime EndDate { get; set; }

    [Required] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedAt { get; set; }

    [Required] public bool UseAccountingStringForScheduler { get; set; } = true;

    [Required] public UsageType UsageType { get; set; } = UsageType.NodeHours;

    public virtual List<AdaptorUserGroup> AdaptorUserGroups { get; set; } = new();
    public virtual List<ClusterProject> ClusterProjects { get; set; } = new();
    public virtual List<CommandTemplate> CommandTemplates { get; set; } = new();
    public virtual List<ProjectContact> ProjectContacts { get; set; } = new();
    public virtual List<SubProject> SubProjects { get; set; } = new();
    public virtual List<AccountingState>? AccountingStates { get; set; }

    public virtual List<ProjectClusterNodeTypeAggregation> ProjectClusterNodeTypeAggregations { get; set; } = new();

    [Required] public bool IsDeleted { get; set; } = false;

    #region Public methods

    public override string ToString()
    {
        return
            $"Project: Id={Id}, Name={Name}, Description={Description}, AccountingString={AccountingString}, StartDate={StartDate}, EndDate={EndDate}, CreatedAt={CreatedAt}, ModifiedAt={ModifiedAt}, IsDeleted={IsDeleted}";
    }

    #endregion
}
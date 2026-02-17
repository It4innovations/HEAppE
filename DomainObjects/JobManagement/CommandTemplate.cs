using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.DomainObjects.JobManagement;

[Table("CommandTemplate")]
public class CommandTemplate : IdentifiableDbEntity
{
    [Required] [StringLength(1000)] public string Name { get; set; }

    [StringLength(1000)] public string Description { get; set; }

    [StringLength(1000)] public string ExtendedAllocationCommand { get; set; }

    [Required] [StringLength(1000)] public string ExecutableFile { get; set; }

    [StringLength(1000)] public string CommandParameters { get; set; }

    [StringLength(1000)] public string PreparationScript { get; set; }

    [Required] public bool IsGeneric { get; set; } = false;

    [Required] public bool IsEnabled { get; set; } = true;

    [Required] public bool IsDeleted { get; set; } = false;

    public virtual List<CommandTemplateParameter> TemplateParameters { get; set; } = new();

    [ForeignKey("ClusterNodeType")] public long? ClusterNodeTypeId { get; set; }

    public virtual ClusterNodeType ClusterNodeType { get; set; }

    [ForeignKey("Project")] public long? ProjectId { get; set; }

    public virtual Project Project { get; set; }

    [ForeignKey("CreatedFrom")] public long? CreatedFromId { get; set; }

    public virtual CommandTemplate CreatedFrom { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }

    public override string ToString()
    {
        return
            $"CommandTemplate({base.ToString()}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; CommandParameters: {CommandParameters}; PreparationScript: {PreparationScript}; IsGeneric: {IsGeneric}; IsEnabled: {IsEnabled}; IsDeleted: {IsDeleted}; ClusterNodeTypeId: {ClusterNodeTypeId}; ProjectId: {ProjectId}; CreatedFromId: {CreatedFromId}; CreatedAt: {CreatedAt}; ModifiedAt: {ModifiedAt})";
    }
}
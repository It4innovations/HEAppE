using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "CreateGenericCommandTemplateModel")]
[Description("Create generic command template model")]
public class CreateGenericCommandTemplateModel : SessionCodeModel
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(10000)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(10000)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Extended allocation command
    /// </summary>
    [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false)]
    [StringLength(10000)]
    [Description("Extended allocation command")]
    public string ExtendedAllocationCommand { get; set; }
    
    /// <summary>
    /// Preparation script
    /// </summary>
    [DataMember(Name = "PreparationScript", IsRequired = false)]
    [StringLength(10000)]
    [Description("Preparation script")]
    public string PreparationScript { get; set; }

    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId", IsRequired = true)]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }
    

    public override string ToString()
    {
        return $"CreateGenericCommandTemplateModel({base.ToString()}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; PreparationScript: {PreparationScript}, ClusterNodeTypeId: {ClusterNodeTypeId}, ProjectId: {ProjectId})";
    }
}
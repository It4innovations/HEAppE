using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify command template model 
/// </summary>
[DataContract(Name = "ModifyCommandTemplateModel")]
[Description("Modify command template model")]
public class ModifyCommandTemplateModel : SessionCodeModel
{
    /// <summary>
    /// Id
    /// </summary>
    [DataMember(Name = "Id", IsRequired = true)]
    [Description("Id")]
    public long Id { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(1000)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = true)]
    [StringLength(1000)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Extended allocation command
    /// </summary>
    [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false)]
    [StringLength(1000)]
    [Description("Extended allocation command")]
    public string ExtendedAllocationCommand { get; set; }

    /// <summary>
    /// Executable file
    /// </summary>
    [DataMember(Name = "ExecutableFile", IsRequired = true)]
    [StringLength(1000)]
    [Description("Executable file")]
    public string ExecutableFile { get; set; }

    /// <summary>
    /// Preparation script
    /// </summary>
    [DataMember(Name = "PreparationScript", IsRequired = false)]
    [StringLength(1000)]
    [Description("Preparation script")]
    public string PreparationScript { get; set; }

    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId", IsRequired = true)]
    [Description("Cluster node type id")]
    public long ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Is enabled
    /// </summary>
    [DataMember(Name = "IsEnabled", IsRequired = true)]
    [Description("Is enabled")]
    public bool IsEnabled { get; set; }

    public override string ToString()
    {
        return $"ModifyCommandTemplateModel({base.ToString()}; Id: {Id}, Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript}, ClusterNodeTypeId: {ClusterNodeTypeId}, IsEnabled: {IsEnabled})";
    }
}
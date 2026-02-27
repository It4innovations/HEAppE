using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Modify command template from generic model
/// </summary>
[DataContract(Name = "ModifyCommandTemplateFromGenericModel")]
[Description("Modify command template from generic model")]
public class ModifyCommandTemplateFromGenericModel : SessionCodeModel
{
    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember(Name = "CommandTemplateId", IsRequired = true)]
    [Description("Command template id")]
    public long CommandTemplateId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = false)]
    [StringLength(1000)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Project id
    /// </summary>
    [DataMember(Name = "ProjectId", IsRequired = true)]
    [Description("Project id")]
    public long ProjectId { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    [DataMember(Name = "Description", IsRequired = false)]
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
    [DataMember(Name = "ExecutableFile", IsRequired = false)]
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

    public override string ToString()
    {
        return $"ModifyCommandTemplateFromGenericModel({base.ToString()}; CommandTemplateId: {CommandTemplateId}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript})";
    }
}
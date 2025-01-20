using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

/// <summary>
/// Create command template from generic model
/// </summary>
[DataContract(Name = "CreateCommandTemplateFromGenericModel")]
[Description("Create command template from generic model")]
public class CreateCommandTemplateFromGenericModel : SessionCodeModel
{
    /// <summary>
    /// Generic command template id
    /// </summary>
    [DataMember(Name = "GenericCommandTemplateId", IsRequired = true)]
    [Description("Generic command template id")]
    public long GenericCommandTemplateId { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name", IsRequired = true)]
    [StringLength(80)]
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
    [DataMember(Name = "Description", IsRequired = true)]
    [StringLength(200)]
    [Description("Description")]
    public string Description { get; set; }

    /// <summary>
    /// Extended allocation command
    /// </summary>
    [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false)]
    [StringLength(100)]
    [Description("Extended allocation command")]
    public string ExtendedAllocationCommand { get; set; }

    /// <summary>
    /// Executable file
    /// </summary>
    [DataMember(Name = "ExecutableFile", IsRequired = true)]
    [StringLength(255)]
    [Description("Executable file")]
    public string ExecutableFile { get; set; }

    /// <summary>
    /// Preparation script
    /// </summary>
    [DataMember(Name = "PreparationScript", IsRequired = false)]
    [StringLength(500)]
    [Description("Preparation script")]
    public string PreparationScript { get; set; }

    public override string ToString()
    {
        return $"CreateCommandTemplateFromGenericModel({base.ToString()}; GenericCommandTemplateId: {GenericCommandTemplateId}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript})";
    }
}
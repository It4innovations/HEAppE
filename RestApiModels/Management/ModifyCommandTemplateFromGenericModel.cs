using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.RestApiModels.AbstractModels;

namespace HEAppE.RestApiModels.Management;

[DataContract(Name = "ModifyCommandTemplateFromGenericModel")]
public class ModifyCommandTemplateFromGenericModel : SessionCodeModel
{
    [DataMember(Name = "CommandTemplateId", IsRequired = true)]
    public long CommandTemplateId { get; set; }

    [DataMember(Name = "Name", IsRequired = false)]
    [StringLength(80)]
    public string Name { get; set; }

    [DataMember(Name = "ProjectId", IsRequired = true)]
    public long ProjectId { get; set; }

    [DataMember(Name = "Description", IsRequired = false)]
    [StringLength(200)]
    public string Description { get; set; }

    [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false)]
    [StringLength(100)]
    public string ExtendedAllocationCommand { get; set; }

    [DataMember(Name = "ExecutableFile", IsRequired = false)]
    [StringLength(255)]
    public string ExecutableFile { get; set; }

    [DataMember(Name = "PreparationScript", IsRequired = false)]
    [StringLength(500)]
    public string PreparationScript { get; set; }

    public override string ToString()
    {
        return
            $"ModifyCommandTemplateFromGenericModel({base.ToString()}; CommandTemplateId: {CommandTemplateId}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript})";
    }
}
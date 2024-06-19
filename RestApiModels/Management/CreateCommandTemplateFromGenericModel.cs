using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateCommandTemplateFromGenericModel")]
    public class CreateCommandTemplateFromGenericModel : SessionCodeModel
    {
        [DataMember(Name = "GenericCommandTemplateId", IsRequired = true)]
        public long GenericCommandTemplateId { get; set; }

        [DataMember(Name = "Name", IsRequired = true), StringLength(80)]
        public string Name { get; set; }

        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }

        [DataMember(Name = "Description", IsRequired = true), StringLength(200)]
        public string Description { get; set; }

        [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false), StringLength(100)]
        public string ExtendedAllocationCommand { get; set; }

        [DataMember(Name = "ExecutableFile", IsRequired = true), StringLength(255)]
        public string ExecutableFile { get; set; }

        [DataMember(Name = "PreparationScript", IsRequired = false), StringLength(500)]
        public string PreparationScript { get; set; }
        public override string ToString()
        {
            return $"CreateCommandTemplateFromGenericModel({base.ToString()}; GenericCommandTemplateId: {GenericCommandTemplateId}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript})";
        }
    }
}

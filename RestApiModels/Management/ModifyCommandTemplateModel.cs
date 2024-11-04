using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ModifyCommandTemplateModel")]
    public class ModifyCommandTemplateModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }
        
        [DataMember(Name = "Name", IsRequired = true), StringLength(80)]
        public string Name { get; set; }
        
        [DataMember(Name = "Description", IsRequired = true), StringLength(200)]
        public string Description { get; set; }
        
        [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false), StringLength(100)]
        public string ExtendedAllocationCommand { get; set; }

        [DataMember(Name = "ExecutableFile", IsRequired = true), StringLength(255)]
        public string ExecutableFile { get; set; }

        [DataMember(Name = "PreparationScript", IsRequired = false), StringLength(500)]
        public string PreparationScript { get; set; }
        
        [DataMember(Name = "ClusterNodeTypeId", IsRequired = true)]
        public long ClusterNodeTypeId { get; set; }
        
        [DataMember(Name = "IsEnabled", IsRequired = true)]
        public bool IsEnabled { get; set; }
        
        public override string ToString()
        {
            return $"ModifyCommandTemplateModel({base.ToString()}; Id: {Id}, Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript}, ClusterNodeTypeId: {ClusterNodeTypeId}, IsEnabled: {IsEnabled})";
        }
    }
}

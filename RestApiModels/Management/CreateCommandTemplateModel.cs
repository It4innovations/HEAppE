using System.Collections.Generic;
using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateCommandTemplateModel")]
    public class CreateCommandTemplateModel : SessionCodeModel
    {
        [DataMember(Name = "Name", IsRequired = true), StringLength(80)]
        public string Name { get; set; }
        
        [DataMember(Name = "Description", IsRequired = false), StringLength(200)]
        public string Description { get; set; }
        
        [DataMember(Name = "ExtendedAllocationCommand", IsRequired = false), StringLength(100)]
        public string ExtendedAllocationCommand { get; set; }

        [DataMember(Name = "ExecutableFile", IsRequired = true), StringLength(255)]
        public string ExecutableFile { get; set; }

        [DataMember(Name = "PreparationScript", IsRequired = false), StringLength(500)]
        public string PreparationScript { get; set; }
        
        [DataMember(Name = "ClusterNodeTypeId", IsRequired = true)]
        public long ClusterNodeTypeId { get; set; }
        
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
        
        [DataMember(Name = "TemplateParameters", IsRequired = false)]
        public List<CreateCommandInnerTemplateParameterModel> TemplateParameters { get; set; }
        public override string ToString()
        {
            return $"CreateCommandTemplateModel({base.ToString()}; Name: {Name}; Description: {Description}; ExtendedAllocationCommand: {ExtendedAllocationCommand}; ExecutableFile: {ExecutableFile}; PreparationScript: {PreparationScript}, ClusterNodeTypeId: {ClusterNodeTypeId}, ProjectId: {ProjectId}, TemplateParameters: {string.Join(",", TemplateParameters.Select(x=>x.ToString()))}";
        }
    }
}

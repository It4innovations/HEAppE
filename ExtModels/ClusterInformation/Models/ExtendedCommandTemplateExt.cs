using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ExtendedCommandTemplateExt")]
    public class ExtendedCommandTemplateExt
    {
        [DataMember(Name = "Id")]
        public long? Id { get; set; }

        [DataMember(Name = "Name")]
        public string Name { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }

        [DataMember(Name = "ExtendedAllocationCommand")]
        public string ExtendedAllocationCommand { get; set; }
        [DataMember(Name = "ExecutableFile")]
        public string ExecutableFile { get; set; }
        [DataMember(Name = "PreparationScript")]
        public string PreparationScript { get; set; }
        [DataMember(Name = "CommandParameters")]
        public string CommandParameters { get; set; }

        [DataMember(Name = "IsGeneric")]
        public bool IsGeneric { get; set; }
        [DataMember(Name = "ProjectId")]
        public long ProjectId { get; set; }
        
        [DataMember(Name = "ClusterNodeTypeId")]
        public long ClusterNodeTypeId { get; set; }

        [DataMember(Name = "TemplateParameters")]
        public ExtendedCommandTemplateParameterExt[] TemplateParameters { get; set; }

        public override string ToString()
        {
            return $"ExtendedCommandTemplateExt(id={Id}; name={Name}; description={Description}; extendedAllocationCommand={ExtendedAllocationCommand}; isGeneric={IsGeneric}; ProjectId={ProjectId}; ClusterNodeTypeId={ClusterNodeTypeId}; templateParameters={string.Join(";", TemplateParameters.Select(x=>x.ToString()))})";
        }
    }
}

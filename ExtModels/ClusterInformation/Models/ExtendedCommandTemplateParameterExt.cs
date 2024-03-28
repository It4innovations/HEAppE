using System.Runtime.Serialization;

namespace HEAppE.ExtModels.ClusterInformation.Models
{
    [DataContract(Name = "ExtendedCommandTemplateParameterExt")]
    public class ExtendedCommandTemplateParameterExt
    {
        [DataMember(Name = "Id")]
        public long Id { get; set; }
        
        [DataMember(Name = "Identifier")]
        public string Identifier { get; set; }

        [DataMember(Name = "Description")]
        public string Description { get; set; }
        
        [DataMember(Name = "Query")]
        public string Query { get; set; }
        
        public override string ToString()
        {
            return $"ExtendedCommandTemplateParameterExt(Id: {Id}, Identifier:{Identifier}, Description: {Description}, Query: {Query})";
        }
    }
}

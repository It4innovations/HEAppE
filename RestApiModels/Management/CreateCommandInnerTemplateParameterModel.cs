using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateCommandInnerTemplateParameterModel")]
    public class CreateCommandInnerTemplateParameterModel
    {
        [DataMember(Name = "Identifier", IsRequired = true), StringLength(20)]
        public string Identifier { get; set; }
        
        [DataMember(Name = "Query", IsRequired = true), StringLength(200)]
        public string Query { get; set; }

        [DataMember(Name = "Description", IsRequired = true), StringLength(200)]
        public string Description { get; set; }
        
        public override string ToString()
        {
            return $"CreateCommandInnerTemplateParameterModel: Identifier={Identifier}, Query={Query}, Description={Description}";
        }
    }
}

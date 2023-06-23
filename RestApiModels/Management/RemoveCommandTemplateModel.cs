using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RemoveCommandTemplateModel")]
    public class RemoveCommandTemplateModel : SessionCodeModel
    {
        [DataMember(Name = "CommandTemplateId", IsRequired = true)]
        public long CommandTemplateId { get; set; }
        public override string ToString()
        {
            return $"RemoveCommandTemplateModel({base.ToString()}; CommandTemplateId: {CommandTemplateId})";
        }
    }
}

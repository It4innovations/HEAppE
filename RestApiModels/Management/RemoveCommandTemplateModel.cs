using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RemoveCommandTemplateModel")]
    public class RemoveCommandTemplateModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long CommandTemplateId { get; set; }

        [DataMember(Name = "DoRestore", IsRequired = false)]
        [DefaultValue(false)]
        public bool DoRestore { get; set; }
        
        public override string ToString()
        {
            return $"RemoveCommandTemplateModel({base.ToString()}; CommandTemplateId: {CommandTemplateId}; DoRestore: {DoRestore})";
        }
    }
}

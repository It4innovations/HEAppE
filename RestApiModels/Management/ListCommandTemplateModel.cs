using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ListCommandTemplateModel")]
    public class ListCommandTemplateModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }
        public override string ToString()
        {
            return $"ListCommandTemplateModel({base.ToString()}; Id: {Id})";
        }
    }
}

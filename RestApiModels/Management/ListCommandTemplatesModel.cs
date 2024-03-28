using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "ListCommandTemplatesModel")]
    public class ListCommandTemplatesModel : SessionCodeModel
    {
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
        public override string ToString()
        {
            return $"ListCommandTemplatesModel({base.ToString()}; ProjectId: {ProjectId})";
        }
    }
}

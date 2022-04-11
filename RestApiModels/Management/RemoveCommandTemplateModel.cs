using HEAppE.RestApiModels.AbstractModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RemoveCommandTemplateModel")]
    public class RemoveCommandTemplateModel : SessionCodeModel
    {
        [DataMember(Name = "CommandTemplateId", IsRequired = true)]
        public long CommandTemplateId { get; set; }
    }
}

using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RecreateSecureShellKeyModel")]
    public class RecreateSecureShellKeyModel : SessionCodeModel
    {
        [DataMember(Name = "Name", IsRequired = true), StringLength(50)]
        public string Username { get; set; }
        [DataMember(Name = "PublicKey", IsRequired = true)]
        public string PublicKey { get; set; }
    }
}

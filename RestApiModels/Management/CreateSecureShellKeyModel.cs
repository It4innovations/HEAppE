using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateSecureShellKeyModel")]
    public class CreateSecureShellKeyModel : SessionCodeModel
    {
        [DataMember(Name = "Name", IsRequired = true)]
        public string Username { get; set; }
        [DataMember(Name = "Projects", IsRequired = true)]
        public long[] Projects { get; set; }
    }
}

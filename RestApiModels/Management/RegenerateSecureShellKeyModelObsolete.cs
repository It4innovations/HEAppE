using HEAppE.RestApiModels.AbstractModels;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "RegenerateSecureShellKeyModel")]
    [Obsolete]
    public class RegenerateSecureShellKeyModelObsolete : SessionCodeModel
    {
        [DataMember(Name = "Password", IsRequired = false), StringLength(50)]
        public string Password { get; set; }
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
        [DataMember(Name = "PublicKey", IsRequired = true)]
        public string PublicKey { get; set; }
    }
}

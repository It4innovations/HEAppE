using System.Collections.Generic;
using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "CreateSecureShellKeyModel")]
    public class CreateSecureShellKeyModel : SessionCodeModel
    {
        [DataMember(Name = "Credentials", IsRequired = true)]
        public List<SshKeyUserCredentialsModel> Credentials { get; set; }
        
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
    }
}

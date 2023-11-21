using HEAppE.RestApiModels.AbstractModels;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "InitializeClusterScriptDirectoryModel")]
    public class InitializeClusterScriptDirectoryModel : SessionCodeModel
    {
        [DataMember(Name = "ProjectId", IsRequired = true)]
        public long ProjectId { get; set; }
        [DataMember(Name = "PublicKey", IsRequired = true)]
        public string PublicKey { get; set; }
        [DataMember(Name = "ClusterProjectRootDirectory", IsRequired = true)]
        public string ClusterProjectRootDirectory { get; set; }
    }
}

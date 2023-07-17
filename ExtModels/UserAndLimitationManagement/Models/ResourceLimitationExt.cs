using System.Runtime.Serialization;

namespace HEAppE.ExtModels.UserAndLimitationManagement.Models
{
    [DataContract(Name = "ResourceLimitationExt")]
    public class ResourceLimitationExt
    {
        [DataMember(Name = "TotalMaxCores")]
        public int? TotalMaxCores { get; set; }

        [DataMember(Name = "MaxCoresPerJob")]
        public int? MaxCoresPerJob { get; set; }

        public override string ToString()
        {
            return $"ResourceLimitationExt(totalMaxCores={TotalMaxCores}; maxCoresPerJob={MaxCoresPerJob})";
        }
    }
}

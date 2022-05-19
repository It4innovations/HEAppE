using System.Runtime.Serialization;

namespace HEAppE.ExtModels.Management.Models
{
    [DataContract(Name = "InstanceInformationExt")]
    public class InstanceInformationExt
    {
        [DataMember(Name = "InstanceName")]
        public string InstanceName { get; set; }

        [DataMember(Name = "Version")]
        public string Version { get; set; }

        [DataMember(Name = "DeployedIPAddress")]
        public string DeployedIPAddress { get; set; }

        public override string ToString()
        {
            return $"InstanceInformationExt: InstanceName={InstanceName}, Version={Version}, DeployedIPAddress={DeployedIPAddress}";
        }
    }
}
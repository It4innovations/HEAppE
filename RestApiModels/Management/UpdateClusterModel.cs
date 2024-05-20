using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.RestApiModels.AbstractModels;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace HEAppE.RestApiModels.Management
{
    [DataContract(Name = "UpdateClusterModel")]
    public class UpdateClusterModel : SessionCodeModel
    {
        [DataMember(Name = "Id", IsRequired = true)]
        public long Id { get; set; }

        [DataMember(Name = "Name", IsRequired = true), StringLength(50)]
        public string Name { get; set; }

        [DataMember(Name = "Description", IsRequired = false), StringLength(100)]
        public string Description { get; set; }

        [DataMember(Name = "MasterNodeName", IsRequired = true), StringLength(100)]
        public string MasterNodeName { get; set; }

        [DataMember(Name = "SchedulerType", IsRequired = true)]
        public SchedulerType SchedulerType { get; set; }

        [DataMember(Name = "ConnectionProtocol", IsRequired = true)]
        public ClusterConnectionProtocol ConnectionProtocol { get; set; }

        [DataMember(Name = "TimeZone", IsRequired = true), StringLength(10)]
        public string TimeZone { get; set; }

        [DataMember(Name = "Port", IsRequired = true)]
        public int Port { get; set; }

        [DataMember(Name = "UpdateJobStateByServiceAccount", IsRequired = true)]
        public bool UpdateJobStateByServiceAccount { get; set; }

        [DataMember(Name = "DomainName", IsRequired = true), StringLength(20)]
        public string DomainName { get; set; }

        [DataMember(Name = "ProxyConnectionId", IsRequired = true)]
        public long? ProxyConnectionId { get; set; }
    }
}

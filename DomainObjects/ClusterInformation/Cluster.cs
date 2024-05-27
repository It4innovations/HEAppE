using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation
{
    [Table("Cluster")]
    public class Cluster : IdentifiableDbEntity, ISoftDeletableEntity
    {
        #region Properties
        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [Required]
        [StringLength(50)]
        public string MasterNodeName { get; set; }

        [StringLength(50)]
        public string DomainName { get; set; }

        public int? Port { get; set; }

        [Required]
        [StringLength(30)]
        public string TimeZone { get; set; } = "UTC";

        public bool? UpdateJobStateByServiceAccount { get; set; } = true;

        [Required]
        public bool IsDeleted { get; set; } = false;

        public SchedulerType SchedulerType { get; set; }

        public virtual ClusterConnectionProtocol ConnectionProtocol { get; set; }

        public virtual List<ClusterNodeType> NodeTypes { get; set; } = new List<ClusterNodeType>();

        public virtual List<FileTransferMethod> FileTransferMethods { get; set; } = new List<FileTransferMethod>();

        [ForeignKey("ClusterProxyConnection")]
        public long? ProxyConnectionId { get; set; }
        public virtual ClusterProxyConnection ProxyConnection { get; set; }
        public virtual List<ClusterProject> ClusterProjects { get; set; } = new List<ClusterProject>();
        #endregion
        #region Override Methods
        public override string ToString()
        {
            return $"Cluster: Id={Id}, Name={Name}, MasterNodeName={MasterNodeName}, Port={Port}, SchedulerType={SchedulerType}, TimeZone={TimeZone}, ConnectionProtocol={ConnectionProtocol}";
        }
        #endregion
    }
}
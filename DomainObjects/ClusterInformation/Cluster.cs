using HEAppE.DomainObjects.FileTransfer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Table("Cluster")]
	public class Cluster : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		[Required]
		[StringLength(200)]
		public string Description { get; set; }

		[Required]
		[StringLength(30)]
		public string MasterNodeName { get; set; }

		[StringLength(30)]
		public string DomainName { get; set; }

		public int? Port { get; set;}

		[Required]
		[StringLength(100)]
		public string LocalBasepath { get; set; }

		[Required]
		[StringLength(30)]
		public string TimeZone { get; set; } = "UTC";

        [ForeignKey("ServiceAccountCredentials")]
        public long? ServiceAccountCredentialsId { get; set; }

		public bool? UpdateJobStateByServiceAccount { get; set; } = true;
		
		public SchedulerType SchedulerType { get; set; }

		public virtual ClusterConnectionProtocol ConnectionProtocol { get; set; }

        [InverseProperty("Cluster")]
        public virtual List<ClusterAuthenticationCredentials> AuthenticationCredentials { get; set; } = new List<ClusterAuthenticationCredentials>();

        public virtual ClusterAuthenticationCredentials ServiceAccountCredentials { get; set; }

		public virtual List<ClusterNodeType> NodeTypes { get; set; } = new List<ClusterNodeType>();

		public virtual List<FileTransferMethod> FileTransferMethods { get; set; } = new List<FileTransferMethod>();

        public override string ToString() {
			return $"Cluster: Id={Id}, Name={Name}, MasterNodeName={MasterNodeName}, Port={Port}, LocalBasepath={LocalBasepath}, SchedulerType={SchedulerType}, TimeZone={TimeZone}, ConnectionProtocol={ConnectionProtocol}";
		}
	}
}
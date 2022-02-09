using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HEAppE.DomainObjects.FileTransfer;
using HEAppE.DomainObjects.JobManagement;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Table("ClusterNodeType")]
	public class ClusterNodeType : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		[Required]
		[StringLength(200)]
		public string Description { get; set; }

		public int? NumberOfNodes { get; set; }

		public int CoresPerNode { get; set; }

		[StringLength(30)]
		public string Queue { get; set; }

		[StringLength(40)]
		public string ClusterAllocationName { get; set; }

		public int? MaxWalltime { get; set; }

        [ForeignKey("Cluster")]
        public long? ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }

        [ForeignKey("FileTransferMethod")]
        public long? FileTransferMethodId { get; set; }
        public virtual FileTransferMethod FileTransferMethod { get; set; }

		public virtual List<ClusterNodeTypeRequestedGroup> RequestedNodeGroups { get; set; } = new List<ClusterNodeTypeRequestedGroup>();

		public virtual List<CommandTemplate> PossibleCommands { get; set; } = new List<CommandTemplate>();

        [ForeignKey("JobTemplate")]
        public long? JobTemplateId { get; set; }
        public virtual JobTemplate JobTemplate { get; set; }

        [ForeignKey("TaskTemplate")]
        public long? TaskTemplateId { get; set; }
        public virtual TaskTemplate TaskTemplate { get; set; }

        public override string ToString() {
			return String.Format("ClusterNodeType: Id={0}, Name={1}, Queue={2}, RequestedNodeGroups={3}, Cluster={4}", Id, Name, Queue, RequestedNodeGroups, Cluster);
		}
	}
}
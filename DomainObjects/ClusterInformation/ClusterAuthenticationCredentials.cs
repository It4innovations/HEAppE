using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Table("ClusterAuthenticationCredentials")]
	public class ClusterAuthenticationCredentials : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Username { get; set; }

		[StringLength(50)]
		public string Password { get; set; }

		[StringLength(200)]
		public string PrivateKeyFile { get; set; }

		[StringLength(50)]
		public string PrivateKeyPassword { get; set; }

        [ForeignKey("Cluster")]
        public long? ClusterId { get; set; }
        public virtual Cluster Cluster { get; set; }

        public override string ToString() {
			return String.Format("ClusterAuthenticationCredentials: Username={0}", Username);
		}
	}
}
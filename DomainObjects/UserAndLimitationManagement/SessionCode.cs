using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.UserAndLimitationManagement {
	[Table("SessionCode")]
	public class SessionCode : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string UniqueCode { get; set; }

		public DateTime AuthenticationTime { get; set; }

		public DateTime LastAccessTime { get; set; }

		public virtual AdaptorUser User { get; set; }
	}
}
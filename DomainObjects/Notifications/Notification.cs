using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.Notifications {
	[Table("Notification")]
	public class Notification : IdentifiableDbEntity {
		[Required]
		[StringLength(100)]
		public string Header { get; set; }

		[Column(TypeName = "text")]
		[Required]
		public string Content { get; set; }

		[StringLength(50)]
		public string Email { get; set; }

		[StringLength(20)]
		public string PhoneNumber { get; set; }

		public DateTime OccurrenceTime { get; set; }

		public DateTime? SentTime { get; set; }

		public virtual Language Language { get; set; }

		public virtual MessageTemplate MessageTemplate { get; set; }
	}
}
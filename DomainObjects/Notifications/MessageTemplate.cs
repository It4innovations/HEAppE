using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.Notifications {
	[Table("MessageTemplate")]
	public class MessageTemplate : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string Name { get; set; }

		[Required]
		[StringLength(200)]
		public string Description { get; set; }

		public NotificationEvent Event { get; set; }

        public virtual List<MessageLocalization> Localizations { get; set; } = new List<MessageLocalization>();

        public virtual List<MessageTemplateParameter> Parameters { get; set; } = new List<MessageTemplateParameter>();
	}
}
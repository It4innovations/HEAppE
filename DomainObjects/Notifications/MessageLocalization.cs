using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.Notifications {
	[Table("MessageLocalization")]
	public class MessageLocalization : IdentifiableDbEntity {
		[Required]
		[StringLength(100)]
		public string LocalizedHeader { get; set; }

		[Column(TypeName = "text")]
		[Required]
		public string LocalizedText { get; set; }

		public virtual Language Language { get; set; }
	}
}
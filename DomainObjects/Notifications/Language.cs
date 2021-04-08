using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.Notifications {
	[Table("Language")]
	public class Language : IdentifiableDbEntity {
		[Required]
		[StringLength(10)]
		public string IsoCode { get; set; }

		[Required]
		[StringLength(50)]
		public string Name { get; set; }
	}
}
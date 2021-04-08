using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement {
	[Table("CommandTemplateParameter")]
	public class CommandTemplateParameter : IdentifiableDbEntity {
		[Required]
		[StringLength(20)]
		public string Identifier { get; set; }

		[Required]
		[StringLength(200)]
		public string Query { get; set; }

		[Required]
		[StringLength(200)]
		public string Description { get; set; }

        [ForeignKey("CommandTemplate")]
        public long? CommandTemplateId { get; set; }
        public virtual CommandTemplate CommandTemplate { get; set; }

        public override string ToString() {
			return String.Format("CommandTemplateParameter: Id={0}, Identifier={1}, Query={2}", Id, Identifier, Query);
		}
	}
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement {
	[Table("CommandTemplateParameterValue")]
	public class CommandTemplateParameterValue : IdentifiableDbEntity {
		[Required]
		[StringLength(1000)]
		public string Value { get; set; }

		[NotMapped]
		public string CommandParameterIdentifier { get; set; }

		public virtual CommandTemplateParameter TemplateParameter { get; set; }

		public override string ToString() {
			return String.Format("CommandTemplateParameterValue: Id={0}, Parameter={1}, Value={2}", Id, TemplateParameter, Value);
		}
	}
}
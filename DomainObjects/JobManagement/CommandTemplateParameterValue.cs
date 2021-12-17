using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
	[Table("CommandTemplateParameterValue")]
	public class CommandTemplateParameterValue : IdentifiableDbEntity
	{
		[Required]
		[StringLength(1000)]
		public string Value { get; set; }

		[NotMapped]
		public string CommandParameterIdentifier { get; set; }

		public virtual CommandTemplateParameter TemplateParameter { get; set; }

		public CommandTemplateParameterValue() : base() { }
		public CommandTemplateParameterValue(CommandTemplateParameterValue commandTemplateParameterValue) : base(commandTemplateParameterValue)
		{
			this.Value = commandTemplateParameterValue.Value;
			this.CommandParameterIdentifier = commandTemplateParameterValue.CommandParameterIdentifier;
			this.TemplateParameter = commandTemplateParameterValue.TemplateParameter;//shallow copy
		}

		public override string ToString()
		{
			return String.Format("CommandTemplateParameterValue: Id={0}, Parameter={1}, Value={2}", Id, TemplateParameter, Value);
		}
	}
}
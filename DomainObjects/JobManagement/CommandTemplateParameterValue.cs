using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("CommandTemplateParameterValue")]
public class CommandTemplateParameterValue : IdentifiableDbEntity
{
    public CommandTemplateParameterValue()
    {
    }

    public CommandTemplateParameterValue(CommandTemplateParameterValue commandTemplateParameterValue) : base(
        commandTemplateParameterValue)
    {
        Value = commandTemplateParameterValue.Value;
        CommandParameterIdentifier = commandTemplateParameterValue.CommandParameterIdentifier;
        TemplateParameter = commandTemplateParameterValue.TemplateParameter; //shallow copy
    }

    [Required] [StringLength(100000)] public string Value { get; set; }

    [NotMapped] public string CommandParameterIdentifier { get; set; }

    public virtual CommandTemplateParameter TemplateParameter { get; set; }

    public override string ToString()
    {
        return string.Format("CommandTemplateParameterValue: Id={0}, Parameter={1}, Value={2}", Id, TemplateParameter,
            Value);
    }
}
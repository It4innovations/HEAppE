using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.ExtModels.General;

namespace HEAppE.RestApiModels.AbstractModels;

public abstract class SessionCodeModel
{
    [DataMember(Name = "SessionCode")]
    [StringLength(50)]
    public string SessionCode { get; set; }

    public override string ToString()
    {
        return $"SessionCodeModel(SessionCode: {SessionCode})";
    }
}

public class SessionCodeModelValidator : AbstractValidator<SessionCodeModel>
{
    public SessionCodeModelValidator()
    {
        RuleFor(x => x.SessionCode).IsSessionCode();
    }
}
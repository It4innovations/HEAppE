using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using FluentValidation;
using HEAppE.ExtModels.General;

namespace HEAppE.RestApiModels.AbstractModels;

/// <summary>
///  Session code model
/// </summary>
[Description("Session code model")]
public abstract class SessionCodeModel
{
    /// <summary>
    /// Session code
    /// </summary>
    [DataMember(Name = "SessionCode")]
    [StringLength(50)]
    [Description("Session code")]
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
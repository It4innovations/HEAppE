using System.ComponentModel;
using System.Runtime.Serialization;
using FluentValidation;

namespace HEAppE.RestApiModels.AbstractModels;

/// <summary>
/// Subbmited job info model
/// </summary>
[Description("Subbmited job info model")]
public abstract class SubmittedJobInfoModel : SessionCodeModel
{
    /// <summary>
    /// Subbmited job info id
    /// </summary>
    [DataMember(Name = "SubmittedJobInfoId")]
    [Description("Subbmited job info id")]
    public long SubmittedJobInfoId { get; set; }

    public override string ToString()
    {
        return $"SubmittedJobInfoModel({base.ToString()}; SubmittedJobInfoId: {SubmittedJobInfoId})";
    }
}

public class SubmittedJobInfoModelValidator : AbstractValidator<SubmittedJobInfoModel>
{
    public SubmittedJobInfoModelValidator()
    {
        Include(new SessionCodeModelValidator());
        RuleFor(x => x.SubmittedJobInfoId).GreaterThan(0);
    }
}
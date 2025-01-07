using System.Runtime.Serialization;
using FluentValidation;

namespace HEAppE.RestApiModels.AbstractModels;

public abstract class SubmittedJobInfoModel : SessionCodeModel
{
    [DataMember(Name = "SubmittedJobInfoId")]
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
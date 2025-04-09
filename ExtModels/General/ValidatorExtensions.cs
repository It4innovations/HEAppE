using FluentValidation;

namespace HEAppE.ExtModels.General;

public static class ValidatorExtensions
{
    public static IRuleBuilder<T, string> IsSessionCode<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        var options = ruleBuilder
            .NotEmpty().WithMessage("SessionCode cannot be empty.")
            .Matches(@"^[0-z]{8}-[0-z]{4}-[0-z]{4}-[0-z]{4}-[0-z]{12}$").WithMessage("SessionCode has wrong format.");

        return options;
    }

    public static IRuleBuilder<T, string> IsCorrectAddress<T>(this IRuleBuilder<T, string> ruleBuilder,
        int maximumLength = 255)
    {
        var options = ruleBuilder
            .NotEmpty()
            .MaximumLength(maximumLength)
            .Matches(@"[^a-zA-Z0-9_\-\ \\\/\.\~]+").WithMessage("SessionCode has wrong format.");

        return options;
    }
}
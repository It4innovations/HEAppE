using System;
using FluentValidation;

namespace HEAppE.ExtModels.General;

public static class ValidatorExtensions
{
    public static IRuleBuilderOptions<T, string> IsSessionCode<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(code => string.IsNullOrEmpty(code) || Guid.TryParse(code, out _))
            .WithMessage("SessionCode must be a valid GUID or empty.");
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
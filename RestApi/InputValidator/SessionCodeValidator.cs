using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class SessionCodeValidator : AbstractValidator
{
    public SessionCodeValidator(object sessionCode) : base(sessionCode)
    {
    }

    public override ValidationResult Validate()
    {
        var message = string.Empty;
        if (!JwtTokenIntrospectionConfiguration.IsEnabled)
        {
            if (_validationObject is null)
            {
                message = "SessionCode cannot be empty.";
            }
            else if (_validationObject is string validationObject)
            {
                message = ValidateSessionCode(validationObject);
            }
        }

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    /// <summary>
    ///     Validates session code string, returns errors
    /// </summary>
    /// <param name="sessionCode"></param>
    /// <returns></returns>
    protected string ValidateSessionCode(string sessionCode)
    {
        if (string.IsNullOrEmpty(sessionCode))
            _messageBuilder.AppendLine("SessionCode cannot be empty.");
        else if (!IsSessionCode(sessionCode)) _messageBuilder.AppendLine("SessionCode has wrong format.");
        return _messageBuilder.ToString();
    }
}
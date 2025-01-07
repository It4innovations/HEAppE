using HEAppE.RestApiModels.UserAndLimitationManagement;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class UserAndLimitationManagementValidator : AbstractValidator
{
    public UserAndLimitationManagementValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            AuthenticateUserOpenIdOpenStackModel model => ValidateAuthenticateUserOpenIdOpenStackModel(model),
            AuthenticateUserOpenIdModel model => ValidateAuthenticateUserOpenIdModel(model),
            AuthenticateUserPasswordModel model => ValidateAuthenticateUserPasswordModel(model),
            AuthenticateUserDigitalSignatureModel model => ValidateAuthenticateUserDigitalSignatureModel(model),
            GetCurrentUsageAndLimitationsForCurrentUserModel model =>
                ValidateCurrentUsageAndLimitationsForCurrentUserModel(model),
            GetProjectsForCurrentUserModel model => ValidateGetProjectsForCurrentUserModel(model),
            AuthenticateLexisTokenModel model => ValidateAuthenticateLexisAuthModel(model),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateGetProjectsForCurrentUserModel(GetProjectsForCurrentUserModel model)
    {
        var validationResult = new SessionCodeValidator(model.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateCurrentUsageAndLimitationsForCurrentUserModel(
        GetCurrentUsageAndLimitationsForCurrentUserModel validationObj)
    {
        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateAuthenticateUserDigitalSignatureModel(AuthenticateUserDigitalSignatureModel validationObj)
    {
        var validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateAuthenticateUserPasswordModel(AuthenticateUserPasswordModel validationObj)
    {
        var validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateAuthenticateUserOpenIdModel(AuthenticateUserOpenIdModel validationObj)
    {
        var validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateAuthenticateLexisAuthModel(AuthenticateLexisTokenModel validationObj)
    {
        var validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateAuthenticateUserOpenIdOpenStackModel(AuthenticateUserOpenIdOpenStackModel validationObj)
    {
        if (validationObj.ProjectId <= 0) _messageBuilder.AppendLine("ProjectId must be greater than 0!");

        var validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }
}
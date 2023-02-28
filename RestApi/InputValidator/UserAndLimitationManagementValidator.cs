using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApiModels.UserAndLimitationManagement;
using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class UserAndLimitationManagementValidator : AbstractValidator
    {
        public UserAndLimitationManagementValidator(object validationObj) : base(validationObj)
        {
        }

        public override ValidationResult Validate()
        {
            string message = _validationObject switch
            {
                AuthenticateUserOpenIdOpenStackModel model => ValidateAuthenticateUserOpenIdOpenStackModel(model),
                AuthenticateUserOpenIdModel model => ValidateAuthenticateUserOpenIdModel(model),
                AuthenticateUserPasswordModel model => ValidateAuthenticateUserPasswordModel(model),
                AuthenticateUserDigitalSignatureModel model => ValidateAuthenticateUserDigitalSignatureModel(model),
                GetCurrentUsageAndLimitationsForCurrentUserModel model => ValidateCurrentUsageAndLimitationsForCurrentUserModel(model),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateCurrentUsageAndLimitationsForCurrentUserModel(GetCurrentUsageAndLimitationsForCurrentUserModel validationObj)
        {
            ValidationResult validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateAuthenticateUserDigitalSignatureModel(AuthenticateUserDigitalSignatureModel validationObj)
        {
            ValidationResult validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateAuthenticateUserPasswordModel(AuthenticateUserPasswordModel validationObj)
        {
            ValidationResult validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateAuthenticateUserOpenIdModel(AuthenticateUserOpenIdModel validationObj)
        {
            ValidationResult validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateAuthenticateUserOpenIdOpenStackModel(AuthenticateUserOpenIdOpenStackModel validationObj)
        {
            if (validationObj.ProjectId <=0)
            {
                _messageBuilder.AppendLine("ProjectId must be greater than 0!");
            }

            ValidationResult validationResult = new CredentialsValidator(validationObj.Credentials).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }
            return _messageBuilder.ToString();
        }
    }
}

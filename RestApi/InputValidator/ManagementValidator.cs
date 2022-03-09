using HEAppE.RestApiModels.Management;
using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class ManagementValidator : AbstractValidator
    {
        public ManagementValidator(object validationObj) : base(validationObj)
        {
        }

        public override ValidationResult Validate()
        {
            string message = _validationObject switch
            {
                CreateCommandTemplateModel ext => ValidateCreateCommandTemplateModel(ext),
                ModifyCommandTemplateModel ext => ValidateModifyCommandTemplateModel(ext),
                RemoveCommandTemplateModel ext => ValidateRemoveCommandTemplateModel(ext),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateRemoveCommandTemplateModel(RemoveCommandTemplateModel model)
        {
            ValidateId(model.CommandTemplateId, nameof(model.CommandTemplateId));
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateModifyCommandTemplateModel(ModifyCommandTemplateModel model)
        {
            ValidateId(model.CommandTemplateId, nameof(model.CommandTemplateId));
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (ContainsIllegalCharactersForPath(model.ExecutableFile))
            {
                _messageBuilder.AppendLine("ExecutableFile contains illegal characters.");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateCreateCommandTemplateModel(CreateCommandTemplateModel model)
        {
            ValidateId(model.GenericCommandTemplateId, nameof(model.GenericCommandTemplateId));
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (ContainsIllegalCharactersForPath(model.ExecutableFile))
            {
                _messageBuilder.AppendLine("ExecutableFile contains illegal characters.");
            }

            return _messageBuilder.ToString();
        }
    }
}

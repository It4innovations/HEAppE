using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.RestApiModels.Management;
using HEAppE.Utils.Validation;
using System;
using System.Linq;

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
                CreateSecureShellKeyModel ext => ValidateCreateSecureShellKeyModel(ext),
                RecreateSecureShellKeyModel ext => ValidateRecreateSecureShellKeyModel(ext),
                RemoveSecureShellKeyModel ext => ValidateRemoveSecureShellKeyModel(ext),
                CreateProjectModel ext => ValidateCreateProjectModel(ext),
                AssignProjectToClusterModel ext => ValidateAssignProjectToClusterModel(ext),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateAssignProjectToClusterModel(AssignProjectToClusterModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            var validationResult = new PathValidator(ext.LocalBasepath).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            ValidateId(ext.ProjectId, "ProjectId");

            ValidateId(ext.ClusterId, "ClusterId");

            return _messageBuilder.ToString();

        }

        private string ValidateCreateProjectModel(CreateProjectModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (string.IsNullOrEmpty(ext.AccountingString))
            {
                _messageBuilder.AppendLine("AccountingString can not be null or empty.");
            }

            if (ext.StartDate > ext.EndDate)
            {
                _messageBuilder.AppendLine("StartDate can not be after EndDate.");
            }

            if (!ext.UsageType.HasValue)
            {
                var validValues = Enum.GetValues(typeof(UsageType)).Cast<UsageType>();
                string validValueString = string.Join(", ", validValues.Select(e => $"{(int)e} ({e})"));
                _messageBuilder.AppendLine($"UsageType must be set, please choose between {validValueString}.");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveSecureShellKeyModel(RemoveSecureShellKeyModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (string.IsNullOrEmpty(ext.PublicKey))
            {
                _messageBuilder.AppendLine("PublicKey can not be null or empty.");
            }
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateRecreateSecureShellKeyModel(RecreateSecureShellKeyModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (string.IsNullOrEmpty(ext.Username))
            {
                _messageBuilder.AppendLine("Username can not be null or empty.");
            }
            if (string.IsNullOrEmpty(ext.PublicKey))
            {
                _messageBuilder.AppendLine("PublicKey can not be null or empty.");
            }
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateCreateSecureShellKeyModel(CreateSecureShellKeyModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (string.IsNullOrEmpty(ext.Username))
            {
                _messageBuilder.AppendLine("Username can not be null or empty.");
            }

            if (ext.Projects == null || ext.Projects.Length == 0)
            {
                _messageBuilder.AppendLine("Projects can not be null or empty.");
            }

            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
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

using HEAppE.DomainObjects.JobReporting.Enums;
using HEAppE.RestApiModels.Management;
using HEAppE.Utils.Validation;
using System;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

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
                ModifyProjectModel ext => ValidateModifyProjectModel(ext),
                RemoveProjectModel ext => ValidateRemoveProjectModel(ext),
                CreateProjectAssignmentToClusterModel ext => ValidateCreateProjectAssignmentToClusterModel(ext),
                ModifyProjectAssignmentToClusterModel ext => ValidateModifyProjectAssignmentToClusterModel(ext),
                RemoveProjectAssignmentToClusterModel ext => ValidateRemoveProjectAssignmentToClusterModel(ext),
                InitializeClusterScriptDirectoryModel ext => ValidateInitializeClusterScriptDirectoryModel(ext),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateInitializeClusterScriptDirectoryModel(InitializeClusterScriptDirectoryModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            var validationResult = new PathValidator(ext.ClusterProjectRootDirectory).Validate();
            
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }
            if (string.IsNullOrEmpty(ext.PublicKey))
            {
                _messageBuilder.AppendLine("PublicKey can not be null or empty.");
            }
            
            ValidateId(ext.ProjectId, "ProjectId");
            return _messageBuilder.ToString();
        }

        private string ValidateCreateProjectAssignmentToClusterModel(CreateProjectAssignmentToClusterModel ext)
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

        private string ValidateModifyProjectAssignmentToClusterModel(ModifyProjectAssignmentToClusterModel ext)
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

        private string ValidateRemoveProjectAssignmentToClusterModel(RemoveProjectAssignmentToClusterModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
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

        private string ValidateModifyProjectModel(ModifyProjectModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
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

            ValidateId(ext.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveProjectModel(RemoveProjectModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(ext.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveSecureShellKeyModel(RemoveSecureShellKeyModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (string.IsNullOrEmpty(ext.PublicKey))
            {
                _messageBuilder.AppendLine("PublicKey can not be null or empty.");
            }
            ValidateId(ext.ProjectId, "ProjectId");
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
            ValidateId(ext.ProjectId, "ProjectId");
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

            ValidateId(ext.ProjectId, "ProjectId");

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

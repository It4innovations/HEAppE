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
                CreateCommandTemplateFromGenericModel ext => ValidateCreateCommandTemplateModel(ext),
                CreateCommandTemplateModel ext => ValidateCreateCommandTemplateModel(ext),
                ModifyCommandTemplateFromGenericModel ext => ValidateModifyCommandTemplateModel(ext),
                ModifyCommandTemplateModel ext => ValidateModifyCommandTemplateModel(ext),
                RemoveCommandTemplateModel ext => ValidateRemoveCommandTemplateModel(ext),
                CreateCommandTemplateParameterModel ext => ValidateCreateCommandTemplateParameterModel(ext),
                ModifyCommandTemplateParameterModel ext => ValidateModifyCommandTemplateParameterModel(ext),
                RemoveCommandTemplateParameterModel ext => ValidateRemoveCommandTemplateParameterModel(ext),
                CreateSecureShellKeyModelObsolete ext => ValidateCreateSecureShellKeyModelObsolete(ext),
                RegenerateSecureShellKeyModelObsolete ext => ValidateRecreateSecureShellKeyModel(ext),
                RemoveSecureShellKeyModelObsolete ext => ValidateRemoveSecureShellKeyModel(ext),
                CreateSecureShellKeyModel ext => ValidateCreateSecureShellKeyModel(ext),
                RegenerateSecureShellKeyModel ext => ValidateRecreateSecureShellKeyModel(ext),
                RemoveSecureShellKeyModel ext => ValidateRemoveSecureShellKeyModel(ext),
                CreateProjectModel ext => ValidateCreateProjectModel(ext),
                ModifyProjectModel ext => ValidateModifyProjectModel(ext),
                RemoveProjectModel ext => ValidateRemoveProjectModel(ext),
                CreateProjectAssignmentToClusterModel ext => ValidateCreateProjectAssignmentToClusterModel(ext),
                ModifyProjectAssignmentToClusterModel ext => ValidateModifyProjectAssignmentToClusterModel(ext),
                RemoveProjectAssignmentToClusterModel ext => ValidateRemoveProjectAssignmentToClusterModel(ext),
                InitializeClusterScriptDirectoryModel ext => ValidateInitializeClusterScriptDirectoryModel(ext),
                TestClusterAccessForAccountModelObsolete ext => ValidateTestClusterAccessForAccountModel(ext),
                TestClusterAccessForAccountModel ext => ValidateTestClusterAccessForAccountModel(ext),
                ListCommandTemplatesModel ext => ValidateListCommandTemplatesModel(ext),
                ListCommandTemplateModel ext => ValidateListCommandTemplateModel(ext),
                ListSubProjectModel ext => ValidateListSubProjectModel(ext),
                ListSubProjectsModel ext => ValidateListSubProjectsModel(ext),
                CreateSubProjectModel ext => ValidateCreateSubProjectModel(ext),
                ModifySubProjectModel ext => ValidateModifySubProjectModel(ext),
                RemoveSubProjectModel ext => ValidateRemoveSubProjectModel(ext),
                ComputeAccountingModel ext => ValidateComputeAccountingModel(ext),
                CreateClusterModel ext => ValidateCreateClusterModel(ext),
                ModifyClusterModel ext => ValidateUpdateClusterModel(ext),
                RemoveClusterModel ext => ValidateRemoveClusterModel(ext),
                CreateClusterNodeTypeModel ext => ValidateCreateClusterNodeTypeModel(ext),
                ModifyClusterNodeTypeModel ext => ValidateModifyClusterNodeTypeModel(ext),
                RemoveClusterNodeTypeModel ext => ValidateRemoveClusterNodeTypeModel(ext),
                CreateClusterProxyConnectionModel ext => ValidateCreateClusterProxyConnectionModel(ext),
                ModifyClusterProxyConnectionModel ext => ValidateModifyClusterProxyConnectionModel(ext),
                RemoveClusterProxyConnectionModel ext => ValidateRemoveClusterProxyConnectionModel(ext),
                CreateFileTransferMethodModel ext => ValidateCreateFileTransferMethodModel(ext),
                ModifyFileTransferMethodModel ext => ValidateModifyFileTransferMethodModel(ext),
                RemoveFileTransferMethodModel ext => ValidateRemoveFileTransferMethodModel(ext),
                CreateClusterNodeTypeAggregationModel ext => ValidateCreateClusterNodeTypeAggregationModel(ext),
                ModifyClusterNodeTypeAggregationModel ext => ValidateModifyClusterNodeTypeAggregationModel(ext),
                RemoveClusterNodeTypeAggregationModel ext => ValidateRemoveClusterNodeTypeAggregationModel(ext),
                CreateClusterNodeTypeAggregationAccountingModel ext => ValidateCreateClusterNodeTypeAggregationAccountingModel(ext),
                RemoveClusterNodeTypeAggregationAccountingModel ext => ValidateRemoveClusterNodeTypeAggregationAccountingModel(ext),
                CreateAccountingModel ext => ValidateCreateAccountingModel(ext),
                ModifyAccountingModel ext => ValidateModifyAccountingModel(ext),
                RemoveAccountingModel ext => ValidateRemoveAccountingModel(ext),
                CreateProjectClusterNodeTypeAggregationModel ext => ValidateCreateProjectClusterNodeTypeAggregationModel(ext),
                ModifyProjectClusterNodeTypeAggregationModel ext => ValidateModifyProjectClusterNodeTypeAggregationModel(ext),
                RemoveProjectClusterNodeTypeAggregationModel ext => ValidateRemoveProjectClusterNodeTypeAggregationModel(ext),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateComputeAccountingModel(ComputeAccountingModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (ext.StartTime > ext.EndTime)
            {
                _messageBuilder.AppendLine("StartTime can not be after EndTime.");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateListSubProjectsModel(ListSubProjectsModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(ext.Id, "Id");
            return _messageBuilder.ToString();
        }

        private string ValidateRemoveSubProjectModel(RemoveSubProjectModel ext)
        {
            ValidateId(ext.Id, "Id");
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateModifySubProjectModel(ModifySubProjectModel ext)
        {
            ValidateId(ext.Id, "Id");
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (string.IsNullOrEmpty(ext.Identifier))
            {
                _messageBuilder.AppendLine("Identifier can not be null or empty.");
            }

            if (ext.StartDate > ext.EndDate)
            {
                _messageBuilder.AppendLine("StartDate can not be after EndDate.");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateCreateSubProjectModel(CreateSubProjectModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (string.IsNullOrEmpty(ext.Identifier))
            {
                _messageBuilder.AppendLine("Identifier can not be null or empty.");
            }

            if (ext.StartDate > ext.EndDate)
            {
                _messageBuilder.AppendLine("StartDate can not be after EndDate.");
            }

            ValidateId(ext.ProjectId, "ProjectId");

            return _messageBuilder.ToString();
        }

        private string ValidateListSubProjectModel(ListSubProjectModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(ext.Id, "Id");
            return _messageBuilder.ToString();
        }

        private string ValidateListCommandTemplateModel(ListCommandTemplateModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(ext.Id, "Id");
            return _messageBuilder.ToString();
        }

        private string ValidateListCommandTemplatesModel(ListCommandTemplatesModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(ext.ProjectId, "ProjectId");
            return _messageBuilder.ToString();
        }

        private string ValidateRemoveCommandTemplateParameterModel(RemoveCommandTemplateParameterModel ext)
        {
            ValidateId(ext.Id, "Id");
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyCommandTemplateParameterModel(ModifyCommandTemplateParameterModel ext)
        {
            
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (string.IsNullOrEmpty(ext.Identifier))
            {
                _messageBuilder.AppendLine("Identifier can not be null or empty.");
            }

            ValidateId(ext.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateCommandTemplateParameterModel(CreateCommandTemplateParameterModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (string.IsNullOrEmpty(ext.Identifier))
            {
                _messageBuilder.AppendLine("Identifier can not be null or empty.");
            }

            ValidateId(ext.CommandTemplateId, "CommandTemplateId");

            return _messageBuilder.ToString();
        }

        private string ValidateTestClusterAccessForAccountModel(TestClusterAccessForAccountModelObsolete ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            
            ValidateId(ext.ProjectId, "ProjectId");
            return _messageBuilder.ToString();
        }

        private string ValidateTestClusterAccessForAccountModel(TestClusterAccessForAccountModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(ext.ProjectId, "ProjectId");
            if (string.IsNullOrEmpty(ext.Username))
            {
                _messageBuilder.AppendLine("Username can not be null or empty.");
            }
            return _messageBuilder.ToString();
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

        private string ValidateCreateSecureShellKeyModelObsolete(CreateSecureShellKeyModelObsolete ext)
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

        private string ValidateRecreateSecureShellKeyModel(RegenerateSecureShellKeyModelObsolete ext)
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

        private string ValidateRemoveSecureShellKeyModel(RemoveSecureShellKeyModelObsolete ext)
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

        private string ValidateCreateSecureShellKeyModel(CreateSecureShellKeyModel ext)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
            foreach(string username in ext.Credentials.Select(x=>x.Username))
            {
                if (string.IsNullOrEmpty(username))
                {
                    _messageBuilder.AppendLine("Username can not be null or empty.");
                }
            }

            ValidateId(ext.ProjectId, "ProjectId");

            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateRecreateSecureShellKeyModel(RegenerateSecureShellKeyModel ext)
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

        private string ValidateRemoveSecureShellKeyModel(RemoveSecureShellKeyModel ext)
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

        private string ValidateModifyCommandTemplateModel(ModifyCommandTemplateFromGenericModel fromGenericModel)
        {
            ValidateId(fromGenericModel.CommandTemplateId, nameof(fromGenericModel.CommandTemplateId));
            ValidationResult sessionCodeValidation = new SessionCodeValidator(fromGenericModel.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (ContainsIllegalCharactersForPath(fromGenericModel.ExecutableFile))
            {
                _messageBuilder.AppendLine("ExecutableFile contains illegal characters.");
            }

            return _messageBuilder.ToString();
        }
        
        private string ValidateModifyCommandTemplateModel(ModifyCommandTemplateModel model)
        {
            ValidateId(model.Id, nameof(model.Id));
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

        private string ValidateCreateCommandTemplateModel(CreateCommandTemplateFromGenericModel fromGenericModel)
        {
            ValidateId(fromGenericModel.GenericCommandTemplateId, nameof(fromGenericModel.GenericCommandTemplateId));
            ValidationResult sessionCodeValidation = new SessionCodeValidator(fromGenericModel.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (ContainsIllegalCharactersForPath(fromGenericModel.ExecutableFile))
            {
                _messageBuilder.AppendLine("ExecutableFile contains illegal characters.");
            }

            return _messageBuilder.ToString();
        }
        
        private string ValidateCreateCommandTemplateModel(CreateCommandTemplateModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (ContainsIllegalCharactersForPath(model.ExecutableFile))
            {
                _messageBuilder.AppendLine("ExecutableFile contains illegal characters.");
            }
            
            //validate template params
            foreach(var parameter in model.TemplateParameters)
            {
                if (string.IsNullOrEmpty(parameter.Identifier))
                {
                    _messageBuilder.AppendLine("Identifier can not be null or empty.");
                }
            }

            return _messageBuilder.ToString();
        }

        private string ValidateCreateClusterModel(CreateClusterModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (model.ProxyConnectionId.HasValue)
            {
                ValidateId(model.ProxyConnectionId, nameof(model.ProxyConnectionId));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateUpdateClusterModel(ModifyClusterModel model)
        {
            ValidateId(model.Id, nameof(model.Id));
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (model.ProxyConnectionId.HasValue)
            {
                ValidateId(model.ProxyConnectionId, nameof(model.ProxyConnectionId));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveClusterModel(RemoveClusterModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateClusterNodeTypeModel(CreateClusterNodeTypeModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (model.NumberOfNodes.HasValue && model.NumberOfNodes <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.NumberOfNodes)));
            }
            if (model.CoresPerNode <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.CoresPerNode)));
            }
            if (model.MaxWalltime.HasValue && model.MaxWalltime <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.MaxWalltime)));
            }
            if (model.ClusterId.HasValue)
            {
                ValidateId(model.ClusterId, nameof(model.ClusterId));
            }
            if (model.FileTransferMethodId.HasValue)
            {
                ValidateId(model.FileTransferMethodId, nameof(model.FileTransferMethodId));
            }
            if (model.ClusterNodeTypeAggregationId.HasValue)
            {
                ValidateId(model.ClusterNodeTypeAggregationId, nameof(model.ClusterNodeTypeAggregationId));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyClusterNodeTypeModel(ModifyClusterNodeTypeModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            if (model.NumberOfNodes.HasValue && model.NumberOfNodes <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.NumberOfNodes)));
            }
            if (model.CoresPerNode <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.CoresPerNode)));
            }
            if (model.MaxWalltime.HasValue && model.MaxWalltime <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.MaxWalltime)));
            }
            if (model.ClusterId.HasValue)
            {
                ValidateId(model.ClusterId, nameof(model.ClusterId));
            }
            if (model.FileTransferMethodId.HasValue)
            {
                ValidateId(model.FileTransferMethodId, nameof(model.FileTransferMethodId));
            }
            if (model.ClusterNodeTypeAggregationId.HasValue)
            {
                ValidateId(model.ClusterNodeTypeAggregationId, nameof(model.ClusterNodeTypeAggregationId));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveClusterNodeTypeModel(RemoveClusterNodeTypeModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateClusterProxyConnectionModel(CreateClusterProxyConnectionModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (model.Port <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.Port)));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyClusterProxyConnectionModel(ModifyClusterProxyConnectionModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            if (model.Port <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.Port)));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveClusterProxyConnectionModel(RemoveClusterProxyConnectionModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateFileTransferMethodModel(CreateFileTransferMethodModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.ClusterId, nameof(model.ClusterId));

            if (model.Port.HasValue && model.Port <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.Port)));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyFileTransferMethodModel(ModifyFileTransferMethodModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");
            ValidateId(model.ClusterId, nameof(model.ClusterId));

            if (model.Port.HasValue && model.Port <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(model.Port)));
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveFileTransferMethodModel(RemoveFileTransferMethodModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateClusterNodeTypeAggregationModel(CreateClusterNodeTypeAggregationModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if(model.ValidityTo.HasValue && model.ValidityTo <= model.ValidityFrom)
            {
                _messageBuilder.AppendLine($"{model.ValidityTo} must be greater than {model.ValidityFrom}");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyClusterNodeTypeAggregationModel(ModifyClusterNodeTypeAggregationModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            if (model.ValidityTo.HasValue && model.ValidityTo <= model.ValidityFrom)
            {
                _messageBuilder.AppendLine($"{model.ValidityTo} must be greater than {model.ValidityFrom}");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveClusterNodeTypeAggregationModel(RemoveClusterNodeTypeAggregationModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateClusterNodeTypeAggregationAccountingModel(CreateClusterNodeTypeAggregationAccountingModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.ClusterNodeTypeAggregationId, "ClusterNodeTypeAggregationId");
            ValidateId(model.AccountingId, "AccountingId");

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveClusterNodeTypeAggregationAccountingModel(RemoveClusterNodeTypeAggregationAccountingModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.ClusterNodeTypeAggregationId, "ClusterNodeTypeAggregationId");
            ValidateId(model.AccountingId, "AccountingId");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateAccountingModel(CreateAccountingModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            if (!AccountingFormulaValidator.IsValidAccountingFormula(model.Formula))
            {
                _messageBuilder.AppendLine($"Not valid formula '{model.Formula}'");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyAccountingModel(ModifyAccountingModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            if (!AccountingFormulaValidator.IsValidAccountingFormula(model.Formula))
            {
                _messageBuilder.AppendLine($"Not valid formula '{model.Formula}'");
            }

            if (model.ValidityTo.HasValue && model.ValidityTo <= model.ValidityFrom)
            {
                _messageBuilder.AppendLine($"{model.ValidityTo} must be greater than {model.ValidityFrom}");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveAccountingModel(RemoveAccountingModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.Id, "Id");

            return _messageBuilder.ToString();
        }

        private string ValidateCreateProjectClusterNodeTypeAggregationModel(CreateProjectClusterNodeTypeAggregationModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.ProjectId, "ProjectId");
            ValidateId(model.ClusterNodeTypeAggregationId, "ClusterNodeTypeAggregationId");

            if (model.AllocationAmount < 0)
            {
                _messageBuilder.AppendLine($"AllocationAmount must be greater than 0");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateModifyProjectClusterNodeTypeAggregationModel(ModifyProjectClusterNodeTypeAggregationModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.ProjectId, "ProjectId");
            ValidateId(model.ClusterNodeTypeAggregationId, "ClusterNodeTypeAggregationId");

            if (model.AllocationAmount < 0)
            {
                _messageBuilder.AppendLine($"AllocationAmount must be greater than 0");
            }

            return _messageBuilder.ToString();
        }

        private string ValidateRemoveProjectClusterNodeTypeAggregationModel(RemoveProjectClusterNodeTypeAggregationModel model)
        {
            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            ValidateId(model.ProjectId, "ProjectId");
            ValidateId(model.ClusterNodeTypeAggregationId, "ClusterNodeTypeAggregationId");

            return _messageBuilder.ToString();
        }
    }
}

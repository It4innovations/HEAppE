using HEAppE.RestApiModels.JobReporting;
using HEAppE.Utils.Validation;
using System;

namespace HEAppE.RestApi.InputValidator
{
    public class JobReportingValidator : AbstractValidator
    {
        public JobReportingValidator(object validationObj) : base(validationObj)
        {

        }

        public override ValidationResult Validate()
        {
            string message = _validationObject switch
            {
                GetUserResourceUsageReportModel model => ValidateGetUserResourceUsageReportModel(model),
                GetResourceUsageReportForJobModel model => ValidateGetResourceUsageReportForJobModel(model),
                GetUserGroupResourceUsageReportModel model => ValidateGetUserGroupResourceUsageReportModel(model),
                GetAggredatedUserGroupResourceUsageReportModel model => ValidateGetAggredatedUserGroupResourceUsageReportModel(model),
                ListAdaptorUserGroupsModel model => ValidateListAdaptorUserGroupsModel(model),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateGetAggredatedUserGroupResourceUsageReportModel(GetAggredatedUserGroupResourceUsageReportModel model)
        {
            ValidationResult validationResult = new SessionCodeValidator(model.SessionCode).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateGetUserGroupResourceUsageReportModel(GetUserGroupResourceUsageReportModel model)
        {
            ValidateId(model.GroupId, nameof(model.GroupId));
            if (model.StartTime > model.EndTime)
            {
                _messageBuilder.AppendLine("StartTime must be before EndTime");
            }

            ValidationResult validationResult = new SessionCodeValidator(model.SessionCode).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateGetResourceUsageReportForJobModel(GetResourceUsageReportForJobModel validationObj)
        {
            ValidateId(validationObj.JobId, nameof(validationObj.JobId));

            ValidationResult validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateGetUserResourceUsageReportModel(GetUserResourceUsageReportModel validationObj)
        {
            ValidateId(validationObj.UserId, nameof(validationObj.UserId));

            if (validationObj.StartTime > validationObj.EndTime)
            {
                _messageBuilder.AppendLine("StartTime must be before EndTime");
            }

            ValidationResult validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateListAdaptorUserGroupsModel(ListAdaptorUserGroupsModel validationObj)
        {
            ValidationResult validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!validationResult.IsValid)
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            return _messageBuilder.ToString();
        }
    }
}

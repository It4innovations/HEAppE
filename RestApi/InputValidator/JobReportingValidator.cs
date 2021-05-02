﻿using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.ExtModels.UserAndLimitationManagement.Models;
using HEAppE.RestApiModels.JobReporting;
using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                //TODO ListAdaptorUserGroupsView? does not exist ListAdaptorUserGroupsView view => ValidateListAdaptorUserGroupsView(view)
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
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

            return validationResult.ToString();
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

            //TODO check if possible send without some of Date
            //result => Yes it is possible (date will be {01.01.0001 0:00:00})

            ValidationResult validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
            {
                _messageBuilder.AppendLine(validationResult.Message);
            }

            return _messageBuilder.ToString();
        }
    }
}

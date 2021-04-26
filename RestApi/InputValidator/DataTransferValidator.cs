using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class DataTransferValidator : AbstractValidator
    {
        public DataTransferValidator(object validationObj) : base(validationObj)
        {
        }

        public override ValidationResult Validate()
        {
            string message = _validationObject switch
            {
                GetDataTransferMethodModel model => ValidateGetDataTransferMethodModel(model),
                EndDataTransferModel model => ValidateEndDataTransferModel(model),
                HttpGetToJobNodeModel model => ValidateHttpGetToJobNodeModel(model),
                HttpPostToJobNodeModel model => ValidateHttpPostToJobNodeModel(model),
                //TODO ,ReadDataFromJobNodeModel, WriteDataToJobNodeModel
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateHttpPostToJobNodeModel(HttpPostToJobNodeModel model)
        {
            if (string.IsNullOrEmpty(model.HttpRequest))
            {
                _messageBuilder.AppendLine("HttpRequest must be set");
            }

            if(model.HttpHeaders.Any(string.IsNullOrEmpty))
            {
                _messageBuilder.AppendLine("HttpHeader cannot be empty");
            }

            if (string.IsNullOrEmpty(model.HttpPayload))
            {
                _messageBuilder.AppendLine("HttpPayload must be set");
            }

            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));

            if (string.IsNullOrEmpty(model.IpAddress))//todo: implement regex for IP
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }

            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateHttpGetToJobNodeModel(HttpGetToJobNodeModel validationObj)
        {
            if (string.IsNullOrEmpty(validationObj.IpAddress))//todo: implement regex for IP
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }

            if (string.IsNullOrEmpty(validationObj.HttpRequest))//todo: implement regex for HttpRequest
            {
                _messageBuilder.AppendLine("HttpRequest must be set");
            }

            if(validationObj.HttpHeaders.Length == 0)
            {
                _messageBuilder.AppendLine("HttpHeader must be set");
            }

            if (validationObj.HttpHeaders.Any(httpHeader => string.IsNullOrEmpty(httpHeader)))
            {
                _messageBuilder.AppendLine("HttpHeader must be set");
            }

            ValidateId(validationObj.SubmittedJobInfoId, nameof(validationObj.SubmittedJobInfoId));

            ValidationResult sessionCodeValidation = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateEndDataTransferModel(EndDataTransferModel validationObj)
        {
            ValidateDataTransferMethodExt(validationObj.UsedTransferMethod);

            ValidationResult sessionCodeValidation = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateGetDataTransferMethodModel(GetDataTransferMethodModel validationObj)
        {
            if (string.IsNullOrEmpty(validationObj.IpAddress))//todo: implement regex for IP (IPv4 and IPv6)
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }

            if (validationObj.Port <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage("Port"));
            }
                
            ValidateId(validationObj.SubmittedJobInfoId, nameof(validationObj.SubmittedJobInfoId));

            ValidationResult sessionCodeValidation = new SessionCodeValidator(validationObj.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateDataTransferMethodExt(DataTransferMethodExt dataTransferMethodExt)
        {
            if (string.IsNullOrEmpty(dataTransferMethodExt.IpAddress))//todo: implement regex for IP
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }

            if (dataTransferMethodExt.Port <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage("Port"));
            }

            ValidateId(dataTransferMethodExt.SubmittedJobId, nameof(dataTransferMethodExt.SubmittedJobId));

            return _messageBuilder.ToString();
        }
    }
}

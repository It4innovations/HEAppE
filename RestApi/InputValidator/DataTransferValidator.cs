﻿using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.Utils.Validation;
using System.Linq;

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
                ReadDataFromJobNodeModel model => ValidateReadDataFromJobNodeModel(model),
                WriteDataToJobNodeModel model => ValidateWriteDataToJobNodeModel(model),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private string ValidateWriteDataToJobNodeModel(WriteDataToJobNodeModel model)
        {
            if (!model.Data.Any())
            {
                _messageBuilder.AppendLine("Data cannot be empty");
            }

            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));

            if (string.IsNullOrEmpty(model.IpAddress))
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }
            else if (!IsIpAddress(model.IpAddress))
            {
                _messageBuilder.AppendLine("Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");
            }

            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }

            return _messageBuilder.ToString();
        }

        private string ValidateReadDataFromJobNodeModel(ReadDataFromJobNodeModel model)
        {
            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));

            if (string.IsNullOrEmpty(model.IpAddress))
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }
            else if (!IsIpAddress(model.IpAddress))
            {
                _messageBuilder.AppendLine("Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");
            }

            ValidationResult sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
            if (!sessionCodeValidation.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidation.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateHttpPostToJobNodeModel(HttpPostToJobNodeModel model)
        {
            if (string.IsNullOrEmpty(model.HttpRequest))
            {
                _messageBuilder.AppendLine("HttpRequest must be set");
            }

            if (model.HttpHeaders.Any(string.IsNullOrEmpty))
            {
                _messageBuilder.AppendLine("HttpHeader cannot be empty");
            }

            if (string.IsNullOrEmpty(model.HttpPayload))
            {
                _messageBuilder.AppendLine("HttpPayload must be set");
            }

            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));

            if (string.IsNullOrEmpty(model.IpAddress))
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }
            else if (!IsIpAddress(model.IpAddress))
            {
                _messageBuilder.AppendLine("Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");
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
            if (string.IsNullOrEmpty(validationObj.IpAddress))
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }
            else if (!IsIpAddress(validationObj.IpAddress))
            {
                _messageBuilder.AppendLine("Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");
            }

            if (string.IsNullOrEmpty(validationObj.HttpRequest))
            {
                _messageBuilder.AppendLine("HttpRequest must be set");
            }

            if (validationObj.HttpHeaders.Length == 0)
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
            if (string.IsNullOrEmpty(validationObj.IpAddress))
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }
            else if (!IsIpAddress(validationObj.IpAddress))
            {
                _messageBuilder.AppendLine("Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");
            }

            ValidatePort(validationObj.Port);

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
            if (string.IsNullOrEmpty(dataTransferMethodExt.IpAddress))
            {
                _messageBuilder.AppendLine("IpAddress must be set");
            }
            else if (!IsIpAddress(dataTransferMethodExt.IpAddress))
            {
                _messageBuilder.AppendLine("Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");
            }

            ValidatePort(dataTransferMethodExt.Port);

            ValidateId(dataTransferMethodExt.SubmittedJobId, nameof(dataTransferMethodExt.SubmittedJobId));

            return _messageBuilder.ToString();
        }


        private void ValidatePort(int port)
        {
            if (port < 0 || port > 65535)
            {
                _messageBuilder.AppendLine("Port must be number between 0 and 65535");
            }
            else if (port == 22)
            {
                _messageBuilder.AppendLine("Port 22 is blocked");
            }
        }
    }
}

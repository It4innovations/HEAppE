using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.Utils.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HEAppE.RestApi.InputValidator
{
    public class FileTransferValidator : AbstractValidator
    {
        public FileTransferValidator(object validationObj) : base(validationObj)
        {
        }

        public override ValidationResult Validate()
        {
            string message = _validationObject switch
            {
                FileTransferMethodExt ext => ValidateFileTransferMethod(ext),
                TaskFileOffsetExt ext => ValidateTaskFileOffset(ext),
                GetFileTransferMethodModel methodModel => ValidateGetFileTransferMethodModel(methodModel),
                EndFileTransferModel transferModel => ValidateEndFileTransferModel(transferModel),
                DownloadPartsOfJobFilesFromClusterModel clusterModel => ValidateDownloadPartsOfJobFilesFromClusterModel(
                    clusterModel),
                ListChangedFilesForJobModel jobModel => ValidateListChangedFilesForJobModel(jobModel),
                DownloadFileFromClusterModel clusterModel => ValidateDownloadFileFromClusterModel(clusterModel),
                _ => string.Empty
            };

            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }

        private void ValidateSessionCode(string sessionCode)
        {
            ValidationResult sessionCodeValidator = new SessionCodeValidator(sessionCode).Validate();
            if (!sessionCodeValidator.IsValid)
            {
                _messageBuilder.AppendLine(sessionCodeValidator.Message);
            }
        }

        private string ValidateDownloadFileFromClusterModel(DownloadFileFromClusterModel model)
        {
            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
            ValidateSessionCode(model.SessionCode);
            ValidationResult pathValidator = new PathValidator(model.RelativeFilePath).Validate();
            if (!pathValidator.IsValid)
            {
                _messageBuilder.AppendLine(pathValidator.Message);
            }
            return _messageBuilder.ToString();
        }

        private string ValidateListChangedFilesForJobModel(ListChangedFilesForJobModel model)
        {
            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
            ValidateSessionCode(model.SessionCode);
            return _messageBuilder.ToString();
        }

        private string ValidateDownloadPartsOfJobFilesFromClusterModel(DownloadPartsOfJobFilesFromClusterModel model)
        {
            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
            ValidateSessionCode(model.SessionCode);

            foreach (var taskFileOffset in model.TaskFileOffsets)
                _ = ValidateTaskFileOffset(taskFileOffset);

            return _messageBuilder.ToString();
        }

        private string ValidateEndFileTransferModel(EndFileTransferModel model)
        {
            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
            ValidateSessionCode(model.SessionCode);
            _ = ValidateFileTransferMethod(model.UsedTransferMethod);

            return _messageBuilder.ToString();
        }

        private string ValidateGetFileTransferMethodModel(GetFileTransferMethodModel model)
        {
            ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
            ValidateSessionCode(model.SessionCode);
            return _messageBuilder.ToString();
        }

        private string ValidateTaskFileOffset(TaskFileOffsetExt taskFileOffset)
        {
            if (taskFileOffset.SubmittedTaskInfoId.HasValue && taskFileOffset.SubmittedTaskInfoId <= 0)
            {
                _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(taskFileOffset.SubmittedTaskInfoId)));
            }
            if(taskFileOffset.Offset.HasValue && taskFileOffset.Offset.Value < 0)
            {
                _messageBuilder.AppendLine("Offset must be positive number");
            }

            //TODO check enum
            return _messageBuilder.ToString();
        }

        private string ValidateFileTransferMethod(FileTransferMethodExt fileTransferMethod)
        {
            ValidationResult credentialsValidator = new CredentialsValidator(fileTransferMethod.Credentials).Validate();
            if (!credentialsValidator.IsValid)
            {
                _messageBuilder.AppendLine(credentialsValidator.Message);
            }

            if (string.IsNullOrEmpty(fileTransferMethod.ServerHostname))
            {
                _messageBuilder.AppendLine("ServerHostname cannot be empty");
            } //Validace IP(vs 4,6) OR DNS

            if (string.IsNullOrEmpty(fileTransferMethod.SharedBasepath))
            {
                _messageBuilder.AppendLine("SharedBasepath cannot be empty");
            }
            else
            {
                ValidationResult pathValidator = new PathValidator(fileTransferMethod.SharedBasepath).Validate();
                if (!pathValidator.IsValid)
                {
                    _messageBuilder.AppendLine(pathValidator.Message);
                }
            }
            //TODO Validation enum (check value for non-exist value)

            return _messageBuilder.ToString();
        }
    }
}

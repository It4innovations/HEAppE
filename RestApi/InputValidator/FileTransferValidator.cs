using HEAppE.ExtModels.FileTransfer.Models;
using HEAppE.RestApiModels.FileTransfer;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class FileTransferValidator : AbstractValidator
{
    public FileTransferValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
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
        var sessionCodeValidator = new SessionCodeValidator(sessionCode).Validate();
        if (!sessionCodeValidator.IsValid) _messageBuilder.AppendLine(sessionCodeValidator.Message);
    }

    private string ValidateDownloadFileFromClusterModel(DownloadFileFromClusterModel model)
    {
        ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
        ValidateSessionCode(model.SessionCode);
        var pathValidator = new PathValidator(model.RelativeFilePath).Validate();
        if (!pathValidator.IsValid) _messageBuilder.AppendLine(pathValidator.Message);
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
        if (string.IsNullOrEmpty(model.PublicKey)) _messageBuilder.AppendLine("PublicKey must be set");

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
            _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage(nameof(taskFileOffset.SubmittedTaskInfoId)));
        if (taskFileOffset.Offset.HasValue && taskFileOffset.Offset.Value < 0)
            _messageBuilder.AppendLine("Offset must be positive number");

        if (!taskFileOffset.FileType.HasValue) _messageBuilder.AppendLine("Protocol must be set");
        return _messageBuilder.ToString();
    }

    private string ValidateFileTransferMethod(FileTransferMethodExt fileTransferMethod)
    {
        if (string.IsNullOrEmpty(fileTransferMethod.ServerHostname))
            _messageBuilder.AppendLine("ServerHostname cannot be empty");
        else if (!IsIpAddress(fileTransferMethod.ServerHostname) &&
                 !IsDomainName(fileTransferMethod.ServerHostname) &&
                 !IsIpAddressWithPort(fileTransferMethod.ServerHostname) &&
                 !IsDomainNamePort(fileTransferMethod.ServerHostname))
            _messageBuilder.AppendLine(
                "ServerHostname has unknown format. If using ipv6, please try to specify 'full address' without shortening.");

        if (string.IsNullOrEmpty(fileTransferMethod.SharedBasepath))
        {
            _messageBuilder.AppendLine("SharedBasePath cannot be empty");
        }
        else
        {
            var pathValidator = new PathValidator(fileTransferMethod.SharedBasepath).Validate();
            if (!pathValidator.IsValid) _messageBuilder.AppendLine(pathValidator.Message);
        }

        if (!fileTransferMethod.Protocol.HasValue) _messageBuilder.AppendLine("Protocol must be set");

        return _messageBuilder.ToString();
    }
}
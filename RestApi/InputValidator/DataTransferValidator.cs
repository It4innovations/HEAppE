using System.Linq;
using HEAppE.ExtModels.DataTransfer.Models;
using HEAppE.RestApiModels.DataTransfer;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class DataTransferValidator : AbstractValidator
{
    public DataTransferValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            GetDataTransferMethodModel model => ValidateGetDataTransferMethodModel(model),
            EndDataTransferModel model => ValidateEndDataTransferModel(model),
            HttpGetToJobNodeModel model => ValidateHttpGetToJobNodeModel(model),
            HttpPostToJobNodeModel model => ValidateHttpPostToJobNodeModel(model),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateHttpPostToJobNodeModel(HttpPostToJobNodeModel model)
    {
        if (string.IsNullOrEmpty(model.HttpRequest)) _messageBuilder.AppendLine("HttpRequest must be set");

        if (model.HttpHeaders.Any(httpHeader =>
                string.IsNullOrEmpty(httpHeader.Name) || string.IsNullOrEmpty(httpHeader.Value)))
            _messageBuilder.AppendLine("HttpHeader cannot be empty");

        ValidatePort(model.NodePort);
        ValidateId(model.SubmittedTaskInfoId, nameof(model.SubmittedTaskInfoId));

        if (string.IsNullOrEmpty(model.NodeIPAddress))
            _messageBuilder.AppendLine("IpAddress must be set");
        else if (!IsIpAddress(model.NodeIPAddress))
            _messageBuilder.AppendLine(
                "Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");

        var sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateHttpGetToJobNodeModel(HttpGetToJobNodeModel validationObj)
    {
        if (string.IsNullOrEmpty(validationObj.NodeIPAddress))
            _messageBuilder.AppendLine("IpAddress must be set");
        else if (!IsIpAddress(validationObj.NodeIPAddress))
            _messageBuilder.AppendLine(
                "Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");

        if (string.IsNullOrEmpty(validationObj.HttpRequest)) _messageBuilder.AppendLine("HttpRequest must be set");

        if (!validationObj.HttpHeaders.Any()) _messageBuilder.AppendLine("HttpHeader must be set");


        if (validationObj.HttpHeaders.Any(httpHeader =>
                string.IsNullOrEmpty(httpHeader.Name) || string.IsNullOrEmpty(httpHeader.Value)))
            _messageBuilder.AppendLine("HttpHeader must be set");


        ValidatePort(validationObj.NodePort);
        ValidateId(validationObj.SubmittedTaskInfoId, nameof(validationObj.SubmittedTaskInfoId));

        var sessionCodeValidation = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateEndDataTransferModel(EndDataTransferModel validationObj)
    {
        ValidateDataTransferMethodExt(validationObj.UsedTransferMethod);

        var sessionCodeValidation = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateGetDataTransferMethodModel(GetDataTransferMethodModel validationObj)
    {
        if (string.IsNullOrEmpty(validationObj.IpAddress))
            _messageBuilder.AppendLine("IpAddress must be set");
        else if (!IsIpAddress(validationObj.IpAddress))
            _messageBuilder.AppendLine(
                "Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");

        ValidatePort(validationObj.Port);

        ValidateId(validationObj.SubmittedTaskInfoId, nameof(validationObj.SubmittedTaskInfoId));

        var sessionCodeValidation = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateDataTransferMethodExt(DataTransferMethodExt dataTransferMethodExt)
    {
        if (string.IsNullOrEmpty(dataTransferMethodExt.NodeIPAddress))
            _messageBuilder.AppendLine("IpAddress must be set");
        else if (!IsIpAddress(dataTransferMethodExt.NodeIPAddress))
            _messageBuilder.AppendLine(
                "Ip address has unknown format. If using ipv6, please try to specify 'full address' without shortening.");

        ValidatePort(dataTransferMethodExt.NodePort);

        ValidateId(dataTransferMethodExt.SubmittedTaskId, nameof(dataTransferMethodExt.SubmittedTaskId));

        return _messageBuilder.ToString();
    }


    private void ValidatePort(int? port)
    {
        if (port is null) _messageBuilder.AppendLine("Port must have value");

        if (port < 0 || port > 65535)
            _messageBuilder.AppendLine("Port must be number between 0 and 65535");
        else if (port == 22) _messageBuilder.AppendLine("Port 22 is blocked");
    }
}
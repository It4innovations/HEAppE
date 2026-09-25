using System.Text.RegularExpressions;
using HEAppE.RestApiModels.ClusterInformation;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class ClusterInformationValidator : AbstractValidator
{
    public ClusterInformationValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            CurrentClusterNodeUsageModel ext => ValidateCurrentClusterNodeUsageModel(ext),
            GetCommandTemplateParametersNameModel ext => ValidateGetCommandTemplateParametersNameModele(ext),
            ListAvailableClustersModel ext => ValidateListAvailableClustersModel(ext),
            GetMachineArchitectureModel ext => ValidateGetMachineArchitectureModel(ext),
            GetMachineInfoModel ext => ValidateGetMachineInfoModel(ext),
            GetMachineCalibrationModel ext => ValidateGetMachineCalibrationModel(ext),
            GetMachineInfoModel ext => ValidateGetMachineInfoModel(ext),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateGetMachineInfoModel(GetMachineInfoModel model)
    {
        ValidateId(model.ClusterNodeTypeId, "ClusterNodeTypeId");
        ValidateId(model.ProjectId, "ProjectId");
        var sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateGetMachineArchitectureModel(GetMachineArchitectureModel model)
    {
        ValidateId(model.ClusterNodeTypeId, "ClusterNodeTypeId");
        ValidateId(model.ProjectId, "ProjectId");
        var sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateGetMachineCalibrationModel(GetMachineCalibrationModel model)
    {
        ValidateId(model.ClusterNodeTypeId, "ClusterNodeTypeId");
        ValidateId(model.ProjectId, "ProjectId");

        // CalibrationId and Endpoint are passed directly into a curl URL path — only safe identifier characters allowed.
        var safeIdentifier = new Regex(@"^[a-zA-Z0-9_-]+$");
        if (string.IsNullOrEmpty(model.CalibrationId))
            _messageBuilder.AppendLine("CalibrationId must be provided.");
        else if (!safeIdentifier.IsMatch(model.CalibrationId))
            _messageBuilder.AppendLine("CalibrationId may only contain alphanumeric characters, hyphens, and underscores.");

        if (string.IsNullOrEmpty(model.Endpoint))
            _messageBuilder.AppendLine("Endpoint must be provided.");
        else if (!safeIdentifier.IsMatch(model.Endpoint))
            _messageBuilder.AppendLine("Endpoint may only contain alphanumeric characters, hyphens, and underscores.");

        var sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateListAvailableClustersModel(ListAvailableClustersModel ext)
    {
        var sessionCodeValidation = new SessionCodeValidator(ext.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateCurrentClusterNodeUsageModel(CurrentClusterNodeUsageModel model)
    {
        ValidateId(model.ClusterNodeId, "ClusterNodeId");
        ValidateId(model.ProjectId, "ProjectId");
        var sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateGetCommandTemplateParametersNameModele(GetCommandTemplateParametersNameModel model)
    {
        if (model.CommandTemplateId <= 0) _messageBuilder.AppendLine(MustBeGreaterThanZeroMessage("CommandTemplateId"));

        if (ContainsIllegalCharactersForPath(model.UserScriptPath))
            _messageBuilder.AppendLine("UserScriptPath contains illegal characters.");

        var sessionCodeValidation = new SessionCodeValidator(model.SessionCode).Validate();
        if (!sessionCodeValidation.IsValid) _messageBuilder.AppendLine(sessionCodeValidation.Message);
        return _messageBuilder.ToString();
    }
}
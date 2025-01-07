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
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
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
using HEAppE.RestApiModels.JobReporting;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class JobReportingValidator : AbstractValidator
{
    public JobReportingValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            UserResourceUsageReportModel model => ValidateUserResourceUsageReportModel(model),
            ResourceUsageReportForJobModel model => ValidateResourceUsageReportForJobModel(model),
            UserGroupResourceUsageReportModel model => ValidateUserGroupResourceUsageReportModel(model),
            GetAggredatedUserGroupResourceUsageReportModel model =>
                ValidateGetAggredatedUserGroupResourceUsageReportModel(model),
            ListAdaptorUserGroupsModel model => ValidateListAdaptorUserGroupsModel(model),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateGetAggredatedUserGroupResourceUsageReportModel(
        GetAggredatedUserGroupResourceUsageReportModel model)
    {
        var validationResult = new SessionCodeValidator(model.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateUserGroupResourceUsageReportModel(UserGroupResourceUsageReportModel model)
    {
        if (model.GroupId == null)
        {
            _messageBuilder.AppendLine("GroupId is required");
        }
        else
        {
            ValidateId(model.GroupId.Value, nameof(model.GroupId));
        }

        if (model.StartTime > model.EndTime) _messageBuilder.AppendLine("StartTime must be before EndTime");

        var validationResult = new SessionCodeValidator(model.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateResourceUsageReportForJobModel(ResourceUsageReportForJobModel validationObj)
    {
        if (validationObj.JobId == null)
        {
            _messageBuilder.AppendLine("JobId is required");
        }
        else
        {
            ValidateId(validationObj.JobId.Value, nameof(validationObj.JobId));
        }

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateUserResourceUsageReportModel(UserResourceUsageReportModel validationObj)
    {
        if (validationObj.UserId == null)
        {
            _messageBuilder.AppendLine("UserId is required");
        }
        else
        {
            ValidateId(validationObj.UserId.Value, nameof(validationObj.UserId));
        }

        if (validationObj.StartTime > validationObj.EndTime)
            _messageBuilder.AppendLine("StartTime must be before EndTime");

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateListAdaptorUserGroupsModel(ListAdaptorUserGroupsModel validationObj)
    {
        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }
}
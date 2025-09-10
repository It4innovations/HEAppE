using System.Linq;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.JobManagement;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

public class JobManagementValidator : AbstractValidator
{
    public JobManagementValidator(object validationObj) : base(validationObj)
    {
    }

    public override ValidationResult Validate()
    {
        var message = _validationObject switch
        {
            CreateJobByProjectModel model => ValidateCreateJobModel(model),
            SubmitJobModel model => ValidateSubmitJobModel(model),
            CancelJobModel model => ValidateCancelJobModel(model),
            DeleteJobModel model => ValidateDeleteJobModel(model),
            ListJobsForCurrentUserModel model => ValidateListJobsForCurrentUserModel(model),
            CurrentInfoForJobModel model => ValidateGetCurrentInfoForJobModel(model),
            CopyJobDataToTempModel model => ValidateCopyJobDataToTempModel(model),
            CopyJobDataFromTempModel model => ValidateCopyJobDataFromTempModel(model),
            AllocatedNodesIPsModel model => ValidateGetAllocatedNodesIPsModel(model),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    private string ValidateGetAllocatedNodesIPsModel(AllocatedNodesIPsModel validationObj)
    {
        ValidateId(validationObj.SubmittedTaskInfoId, nameof(validationObj.SubmittedTaskInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateCopyJobDataFromTempModel(CopyJobDataFromTempModel validationObj)
    {
        ValidateId(validationObj.CreatedJobInfoId, nameof(validationObj.CreatedJobInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        validationResult = new SessionCodeValidator(validationObj.TempSessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateCopyJobDataToTempModel(CopyJobDataToTempModel validationObj)
    {
        ValidateId(validationObj.SubmittedJobInfoId, nameof(validationObj.SubmittedJobInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        validationResult = new PathValidator(validationObj.Path).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateGetCurrentInfoForJobModel(CurrentInfoForJobModel validationObj)
    {
        ValidateId(validationObj.SubmittedJobInfoId, nameof(validationObj.SubmittedJobInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateListJobsForCurrentUserModel(ListJobsForCurrentUserModel validationObj)
    {
        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateDeleteJobModel(DeleteJobModel validationObj)
    {
        ValidateId(validationObj.SubmittedJobInfoId, nameof(validationObj.SubmittedJobInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateCancelJobModel(CancelJobModel validationObj)
    {
        ValidateId(validationObj.SubmittedJobInfoId, nameof(validationObj.SubmittedJobInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateSubmitJobModel(SubmitJobModel validationObj)
    {
        ValidateId(validationObj.CreatedJobInfoId, nameof(validationObj.CreatedJobInfoId));

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateCreateJobModel(CreateJobByProjectModel validationObj)
    {
        _ = ValidateJobSpecificationExt(validationObj.JobSpecification);
        if (validationObj.JobSpecification.ProjectId <= 0)
            _messageBuilder.AppendLine("ProjectId must be greater than 0.");

        var validationResult = new SessionCodeValidator(validationObj.SessionCode).Validate();
        if (!validationResult.IsValid) _messageBuilder.AppendLine(validationResult.Message);
        return _messageBuilder.ToString();
    }

    private string ValidateJobSpecificationExt(JobSpecificationExt job)
    {
        if (string.IsNullOrEmpty(job.Name))
        {
            _messageBuilder.AppendLine("Name cannot be empty.");
        }
        else
        {
            if (ContainsIllegalCharacters(job.Name)) _messageBuilder.AppendLine("Name contains illegal characters.");
        }

        if (job.WaitingLimit.HasValue && job.WaitingLimit.Value < 0)
            _messageBuilder.AppendLine("WaitingLimit must be unsigned number");

        if (job.WalltimeLimit.HasValue && job.WalltimeLimit.Value < 0)
            _messageBuilder.AppendLine("WalltimeLimit must be unsigned number");

        if (!string.IsNullOrEmpty(job.NotificationEmail))
        {
            if (job.NotificationEmail.Length > 50)
                _messageBuilder.AppendLine("Notification email cannot be longer than 50 characters.");

            if (!IsEmailAddress(job.NotificationEmail))
                _messageBuilder.AppendLine("Notification email address is in a wrong format.");
        }

        if (!string.IsNullOrEmpty(job.PhoneNumber))
        {
            if (job.PhoneNumber.Length > 20)
                _messageBuilder.AppendLine("Phone number cannot be longer than 20 characters.");

            if (!IsPhoneNumber(job.PhoneNumber)) _messageBuilder.AppendLine("Phone number is in a wrong format.");
        }

        if (job.EnvironmentVariables != null)
            foreach (var variable in job.EnvironmentVariables)
                if (string.IsNullOrEmpty(variable.Name))
                    _messageBuilder.AppendLine(
                        $"Environment variable's name for the job cannot be empty. ({variable.Name}={variable.Value})");

        if (job.Tasks == null || job.Tasks.Length == 0)
            _messageBuilder.AppendLine("Each job has to contain at least one task.");
        else
            foreach (var task in job.Tasks)
                ValidateTaskSpecificationInput(task);

        return _messageBuilder.ToString();
    }

    private void ValidateTaskSpecificationInput(TaskSpecificationExt task)
    {
        if (string.IsNullOrEmpty(task.Name)) _messageBuilder.AppendLine("Task name cannot be empty.");

        if (ContainsIllegalCharacters(task.Name)) _messageBuilder.AppendLine("Task name contains illegal characters.");

        if (task.MinCores <= 0)
            _messageBuilder.AppendLine($"Minimal number of cores for task \"{task.Name}\" has to be greater than 0.");

        if (task.MaxCores <= 0)
            _messageBuilder.AppendLine($"Maximal number of cores for task \"{task.Name}\" has to be greater than 0.");

        if (task.MinCores > task.MaxCores)
            _messageBuilder.AppendLine(
                $"Minimal number of cores for task \"{task.Name}\" cannot be greater than maximal number of cores.");

        if (task.WalltimeLimit <= 0)
            _messageBuilder.AppendLine($"Walltime limit for task \"{task.Name}\" has to be greater than 0.");

        if (task.JobArrays != null)
        {
            if (task.JobArrays == string.Empty)
            {
                task.JobArrays = null;
            }
            else
            {
                if (task.JobArrays.Length > 40)
                    _messageBuilder.AppendLine(
                        $"JobArrays specification for task \"{task.Name}\" cannot be longer than 40 characters.");

                task.JobArrays = task.JobArrays.Replace(" ", string.Empty);
                var splittedArray = task.JobArrays.Split(':');
                if (splittedArray.Length >= 1)
                {
                    var step = 1;
                    var interval = splittedArray[0].Split('-');
                    if ((splittedArray.Length == 1 ||
                         (splittedArray.Length == 2 && int.TryParse(splittedArray[1], out step)))
                        && interval.Length == 2 && int.TryParse(interval[0], out var min) &&
                        int.TryParse(interval[1], out var max))
                    {
                        if (!(min < max && min + step <= max))
                            _messageBuilder.AppendLine(
                                $"JobArrays parameter for task \"{task.Name}\" has wrong filled minimum or maximum or step value.");
                    }
                    else
                    {
                        _messageBuilder.AppendLine(
                            $"JobArrays parameter for task \"{task.Name}\" has wrong definition.");
                    }
                }
            }
        }

        if (task.TaskParallelizationParameters?.Sum(s => s.MaxCores) > task.MaxCores)
            _messageBuilder.AppendLine(
                $"TaskParalizationSpecifications count of maximal cores for task \"{task.Name}\" must be lower or equals to Maximal number of cores in task.");

        if (!string.IsNullOrEmpty(task.PlacementPolicy))
            if (task.PlacementPolicy.Length > 40)
                _messageBuilder.AppendLine(
                    $"Placement policy specification for task \"{task.Name}\" cannot be longer than 40 characters.");

        if (!string.IsNullOrEmpty(task.StandardInputFile))
        {
            if (task.StandardInputFile.Length > 30)
                _messageBuilder.AppendLine(
                    $"Standard input file for task \"{task.Name}\" cannot be longer than 30 characters.");

            if (ContainsIllegalCharacters(task.StandardInputFile))
                _messageBuilder.AppendLine(
                    $"Standard input file for task \"{task.Name}\" contains illegal characters.");
        }

        if (!string.IsNullOrEmpty(task.StandardOutputFile))
        {
            if (task.StandardOutputFile.Length > 30)
                _messageBuilder.AppendLine(
                    $"Standard output file for task \"{task.Name}\" cannot be longer than 30 characters.");

            if (ContainsIllegalCharacters(task.StandardOutputFile))
                _messageBuilder.AppendLine(
                    $"Standard output file for task \"{task.Name}\" contains illegal characters.");
        }

        if (!string.IsNullOrEmpty(task.StandardErrorFile))
        {
            if (task.StandardErrorFile.Length > 30)
                _messageBuilder.AppendLine(
                    $"Standard error file for task \"{task.Name}\" cannot be longer than 30 characters.");

            if (ContainsIllegalCharacters(task.StandardErrorFile))
                _messageBuilder.AppendLine(
                    $"Standard error file for task \"{task.Name}\" contains illegal characters.");
        }

        if (!string.IsNullOrEmpty(task.ProgressFile))
        {
            if (task.ProgressFile.Length > 50)
                _messageBuilder.AppendLine(
                    $"Progress file for task \"{task.Name}\" cannot be longer than 50 characters.");

            if (ContainsIllegalCharactersForPath(task.ProgressFile))
                _messageBuilder.AppendLine($"Progress file for task \"{task.Name}\" contains illegal characters.");
        }
        else
        {
            _messageBuilder.AppendLine($"Progress file for task \"{task.Name}\" is not set.");
        }

        if (!string.IsNullOrEmpty(task.LogFile))
        {
            if (task.LogFile.Length > 50)
                _messageBuilder.AppendLine($"Log file for task \"{task.Name}\" cannot be longer than 50 characters.");
            if (ContainsIllegalCharactersForPath(task.LogFile))
                _messageBuilder.AppendLine($"Log file for task \"{task.Name}\" contains illegal characters.");
        }
        else
        {
            _messageBuilder.AppendLine($"Log file for task \"{task.Name}\" is not set.");
        }

        if (!string.IsNullOrEmpty(task.ClusterTaskSubdirectory))
        {
            if (task.ClusterTaskSubdirectory.Length > 50)
                _messageBuilder.AppendLine(
                    $"Cluster task subdirectory for task \"{task.Name}\" cannot be longer than 50 characters.");

            if (ContainsIllegalCharacters(task.ClusterTaskSubdirectory))
                _messageBuilder.AppendLine(
                    $"Cluster task subdirectory for task \"{task.Name}\" contains illegal characters.");
        }

        if (task.RequiredNodes != null)
            foreach (var requiredNode in task.RequiredNodes)
            {
                if (requiredNode.Length > 40)
                    _messageBuilder.AppendLine(
                        $"Required node \"{requiredNode}\" specification for task \"{task.Name}\" cannot be longer than 40 characters.");
                if (ContainsIllegalCharacters(requiredNode))
                    _messageBuilder.AppendLine(
                        $"Required node \"{requiredNode}\" specification for task \"{task.Name}\" contains illegal characters.");
            }

        if (task.TemplateParameterValues != null)
        {
            foreach (var parameter in task.TemplateParameterValues)
            {
                if (parameter.CommandParameterIdentifier == null)
                {
                    _messageBuilder.AppendLine(
                        $"Parameter identifier for parameter in task \"{task.Name}\" cannot be null.");
                }
                if (parameter.ParameterValue == null)
                {
                    _messageBuilder.AppendLine(
                        $"Parameter value for parameter \"{parameter.CommandParameterIdentifier}\" in task \"{task.Name}\" cannot be null.");
                }
            }
        }
        if (task.EnvironmentVariables != null)
            foreach (var variable in task.EnvironmentVariables)
                if (string.IsNullOrEmpty(variable.Name))
                    _messageBuilder.AppendLine(
                        $"Environment variable's name for task \"{task.Name}\" cannot be empty. ({variable.Name} = {variable.Value})");
    }
}
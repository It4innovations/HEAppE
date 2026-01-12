using System.Linq;
using HEAppE.ExtModels.JobManagement.Models;
using HEAppE.RestApiModels.JobManagement;
using HEAppE.Utils.Validation;

namespace HEAppE.RestApi.InputValidator;

/// <summary>
/// Validator for Job Management API models.
/// Validates inputs for creating, submitting, cancelling, and querying jobs.
/// </summary>
public class JobManagementValidator : AbstractValidator
{
    public JobManagementValidator(object validationObj) : base(validationObj)
    {
    }

    /// <summary>
    /// entry point for validation. Dispatches to specific validation methods based on the model type.
    /// </summary>
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
            DryRunJobModel model => ValidateDryRunJobModel(model),
            _ => string.Empty
        };

        return new ValidationResult(string.IsNullOrEmpty(message), message);
    }

    // --- Specific Model Validators ---

    private string ValidateDryRunJobModel(DryRunJobModel model)
    {
        ValidateSessionCode(model.SessionCode);

        // Validate numeric constraints
        ValidatePositiveId(model.ProjectId, "ProjectId");
        ValidatePositiveId(model.ClusterNodeTypeId, "ClusterNodeTypeId");
        ValidatePositiveId(model.Nodes, "Nodes");
        ValidatePositiveId(model.TasksPerNode, "TasksPerNode");
        ValidatePositiveId(model.WallTimeInMinutes, "WallTimeInMinutes");

        return _messageBuilder.ToString();
    }

    private string ValidateGetAllocatedNodesIPsModel(AllocatedNodesIPsModel model)
    {
        ValidateId(model.SubmittedTaskInfoId, nameof(model.SubmittedTaskInfoId));
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    private string ValidateCopyJobDataFromTempModel(CopyJobDataFromTempModel model)
    {
        ValidateId(model.CreatedJobInfoId, nameof(model.CreatedJobInfoId));
        ValidateSessionCode(model.SessionCode);
        ValidateSessionCode(model.TempSessionCode); // Validates the second session code
        return _messageBuilder.ToString();
    }

    private string ValidateCopyJobDataToTempModel(CopyJobDataToTempModel model)
    {
        ValidateId(model.CreatedJobInfoId, nameof(model.CreatedJobInfoId));
        ValidateSessionCode(model.SessionCode);

        // Validate the path specifically
        var pathValidation = new PathValidator(model.Path).Validate();
        if (!pathValidation.IsValid) _messageBuilder.AppendLine(pathValidation.Message);

        return _messageBuilder.ToString();
    }

    private string ValidateGetCurrentInfoForJobModel(CurrentInfoForJobModel model)
    {
        ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    private string ValidateListJobsForCurrentUserModel(ListJobsForCurrentUserModel model)
    {
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    private string ValidateDeleteJobModel(DeleteJobModel model)
    {
        ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    private string ValidateCancelJobModel(CancelJobModel model)
    {
        ValidateId(model.SubmittedJobInfoId, nameof(model.SubmittedJobInfoId));
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    private string ValidateSubmitJobModel(SubmitJobModel model)
    {
        ValidateId(model.CreatedJobInfoId, nameof(model.CreatedJobInfoId));
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    private string ValidateCreateJobModel(CreateJobByProjectModel model)
    {
        ValidateJobSpecificationExt(model.JobSpecification);
        ValidatePositiveId(model.JobSpecification.ProjectId, "ProjectId");
        ValidateSessionCode(model.SessionCode);
        return _messageBuilder.ToString();
    }

    // --- Complex Object Validators ---

    private string ValidateJobSpecificationExt(JobSpecificationExt job)
    {
        // Name validation
        if (string.IsNullOrEmpty(job.Name))
            _messageBuilder.AppendLine("Name cannot be empty.");
        else if (ContainsIllegalCharacters(job.Name)) 
            _messageBuilder.AppendLine("Name contains illegal characters.");

        // Numeric limits validation
        if (job.WaitingLimit is < 0)
            _messageBuilder.AppendLine("WaitingLimit must be unsigned number");

        if (job.WalltimeLimit is < 0)
            _messageBuilder.AppendLine("WalltimeLimit must be unsigned number");

        // Contact info validation
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
            if (!IsPhoneNumber(job.PhoneNumber)) 
                _messageBuilder.AppendLine("Phone number is in a wrong format.");
        }

        // Environment variables validation
        if (job.EnvironmentVariables != null)
        {
            foreach (var variable in job.EnvironmentVariables)
            {
                if (string.IsNullOrEmpty(variable.Name))
                    _messageBuilder.AppendLine($"Environment variable's name for the job cannot be empty. ({variable.Name}={variable.Value})");
            }
        }

        // Tasks validation
        if (job.Tasks == null || job.Tasks.Length == 0)
        {
            _messageBuilder.AppendLine("Each job has to contain at least one task.");
        }
        else
        {
            foreach (var task in job.Tasks)
                ValidateTaskSpecificationInput(task);
        }

        return _messageBuilder.ToString();
    }

    private void ValidateTaskSpecificationInput(TaskSpecificationExt task)
    {
        // Basic name validation
        if (string.IsNullOrEmpty(task.Name)) 
            _messageBuilder.AppendLine("Task name cannot be empty.");
        if (ContainsIllegalCharacters(task.Name)) 
            _messageBuilder.AppendLine("Task name contains illegal characters.");

        // Core and Walltime constraints
        if (task.MinCores <= 0)
            _messageBuilder.AppendLine($"Minimal number of cores for task \"{task.Name}\" has to be greater than 0.");
        if (task.MaxCores <= 0)
            _messageBuilder.AppendLine($"Maximal number of cores for task \"{task.Name}\" has to be greater than 0.");
        if (task.MinCores > task.MaxCores)
            _messageBuilder.AppendLine($"Minimal number of cores for task \"{task.Name}\" cannot be greater than maximal number of cores.");
        if (task.WalltimeLimit <= 0)
            _messageBuilder.AppendLine($"Walltime limit for task \"{task.Name}\" has to be greater than 0.");

        // Job Arrays parsing and validation
        if (task.JobArrays != null)
        {
            if (task.JobArrays == string.Empty)
            {
                task.JobArrays = null;
            }
            else
            {
                if (task.JobArrays.Length > 40)
                    _messageBuilder.AppendLine($"JobArrays specification for task \"{task.Name}\" cannot be longer than 40 characters.");

                task.JobArrays = task.JobArrays.Replace(" ", string.Empty);
                var splittedArray = task.JobArrays.Split(':');
                
                // Logic to parse range (min-max) and step
                if (splittedArray.Length >= 1)
                {
                    int step = 1;
                    int min = 0;
                    int max = 0;
                    var interval = splittedArray[0].Split('-');

                    bool isStepValid = splittedArray.Length == 1 || (splittedArray.Length == 2 && int.TryParse(splittedArray[1], out step));
            
                    // Use existing variables 'min' and 'max' instead of 'out var'
                    bool isIntervalValid = interval.Length == 2 
                                           && int.TryParse(interval[0], out min) 
                                           && int.TryParse(interval[1], out max);

                    if (isStepValid && isIntervalValid)
                    {
                        if (!(min < max && min + step <= max))
                            _messageBuilder.AppendLine($"JobArrays parameter for task \"{task.Name}\" has wrong filled minimum or maximum or step value.");
                    }
                    else
                    {
                        _messageBuilder.AppendLine($"JobArrays parameter for task \"{task.Name}\" has wrong definition.");
                    }
                }
            }
        }

        // Parallelization parameters check
        if (task.TaskParallelizationParameters?.Sum(s => s.MaxCores) > task.MaxCores)
            _messageBuilder.AppendLine($"TaskParalizationSpecifications count of maximal cores for task \"{task.Name}\" must be lower or equals to Maximal number of cores in task.");

        // Placement Policy
        ValidateStringAttribute(task.PlacementPolicy, "Placement policy specification", task.Name, 40, checkPathChars: false, isMandatory: false);

        // Standard Files Validation
        ValidateStringAttribute(task.StandardInputFile, "Standard input file", task.Name, 30, checkPathChars: false, isMandatory: false);
        ValidateStringAttribute(task.StandardOutputFile, "Standard output file", task.Name, 30, checkPathChars: false, isMandatory: false);
        ValidateStringAttribute(task.StandardErrorFile, "Standard error file", task.Name, 30, checkPathChars: false, isMandatory: false);

        // Progress File
        if (string.IsNullOrEmpty(task.ProgressFile))
            _messageBuilder.AppendLine($"Progress file for task \"{task.Name}\" is not set.");
        else
            ValidateStringAttribute(task.ProgressFile, "Progress file", task.Name, 50, checkPathChars: true, isMandatory: true);

        // Log File
        if (string.IsNullOrEmpty(task.LogFile))
            _messageBuilder.AppendLine($"Log file for task \"{task.Name}\" is not set.");
        else
            ValidateStringAttribute(task.LogFile, "Log file", task.Name, 50, checkPathChars: true, isMandatory: true);

        // Cluster Subdirectory
        ValidateStringAttribute(task.ClusterTaskSubdirectory, "Cluster task subdirectory", task.Name, 50, checkPathChars: false, isMandatory: false);

        // Required Nodes
        if (task.RequiredNodes != null)
        {
            foreach (var requiredNode in task.RequiredNodes)
            {
                ValidateStringAttribute(requiredNode, $"Required node \"{requiredNode}\" specification", task.Name, 40, checkPathChars: false, isMandatory: true);
            }
        }

        // Template Parameters
        if (task.TemplateParameterValues != null)
        {
            foreach (var parameter in task.TemplateParameterValues)
            {
                if (parameter.CommandParameterIdentifier == null)
                    _messageBuilder.AppendLine($"Parameter identifier for parameter in task \"{task.Name}\" cannot be null.");
                
                if (parameter.ParameterValue == null)
                    _messageBuilder.AppendLine($"Parameter value for parameter \"{parameter.CommandParameterIdentifier}\" in task \"{task.Name}\" cannot be null.");
            }
        }

        // Task Environment Variables
        if (task.EnvironmentVariables != null)
        {
            foreach (var variable in task.EnvironmentVariables)
            {
                if (string.IsNullOrEmpty(variable.Name))
                    _messageBuilder.AppendLine($"Environment variable's name for task \"{task.Name}\" cannot be empty. ({variable.Name} = {variable.Value})");
            }
        }
    }

    // --- Helper Methods to Reduce Duplication ---

    /// <summary>
    /// Helper to validate session code using the SessionCodeValidator.
    /// </summary>
    private void ValidateSessionCode(string sessionCode)
    {
        var result = new SessionCodeValidator(sessionCode).Validate();
        if (!result.IsValid) _messageBuilder.AppendLine(result.Message);
    }

    /// <summary>
    /// Helper to validate positive integers (IDs, counts, limits).
    /// </summary>
    private void ValidatePositiveId(long value, string fieldName)
    {
        if (value <= 0)
            _messageBuilder.AppendLine($"{fieldName} must be greater than 0.");
    }

    /// <summary>
    /// Helper to validate string attributes like file paths or names.
    /// Checks length and illegal characters.
    /// </summary>
    private void ValidateStringAttribute(string value, string fieldDescription, string taskName, int maxLength, bool checkPathChars, bool isMandatory)
    {
        if (string.IsNullOrEmpty(value)) return; // If not mandatory and empty, skip checks. Missing mandatory checks are handled by caller for specific "is not set" messages.

        if (value.Length > maxLength)
            _messageBuilder.AppendLine($"{fieldDescription} for task \"{taskName}\" cannot be longer than {maxLength} characters.");

        bool hasIllegalChars = checkPathChars ? ContainsIllegalCharactersForPath(value) : ContainsIllegalCharacters(value);
        if (hasIllegalChars)
            _messageBuilder.AppendLine($"{fieldDescription} for task \"{taskName}\" contains illegal characters.");
    }
}
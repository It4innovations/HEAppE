using System.Linq;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.Utils.Validation;

namespace HEAppE.ServiceTier.JobManagement.Validators
{
    /// <summary>
    /// Job specification validator
    /// </summary>
#warning Must be at REST API
    internal class JobSpecificationValidator : AbstractValidator
    {
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="validationObj">Validation Object</param>
        internal JobSpecificationValidator(object validationObj) : base(validationObj)
        {
        }
        #endregion
        #region Override Methods
        /// <summary>
        /// Validation
        /// </summary>
        /// <returns></returns>
        public override ValidationResult Validate()
        {
            var validationObj = (JobSpecification)_validationObject;
            var message = ValidateJobSpecificationInput(validationObj);
            return new ValidationResult(string.IsNullOrEmpty(message), message);
        }
        #endregion
        #region Local Methods
        /// <summary>
        /// Validate Job specification
        /// </summary>
        /// <param name="job">Job specification</param>
        /// <returns></returns>
        private string ValidateJobSpecificationInput(JobSpecification job)
        {
            if (!(job.FileTransferMethodId.HasValue))
            {
                _messageBuilder.AppendLine("FileTransferMethod is empty.");
            }

            if (job.ClusterId <= 0)
            {
                _messageBuilder.AppendLine("ClusterId cannot be empty or <= 0.");
            }

            if (string.IsNullOrEmpty(job.Name))
                _messageBuilder.AppendLine("Job name cannot be empty.");

            if (job.Name.Length > 50)
                _messageBuilder.AppendLine("Job name cannot be longer than 50 characters.");

            if (ContainsIllegalCharacters(job.Name))
                _messageBuilder.AppendLine("Job name contains illegal characters.");

            if (!string.IsNullOrEmpty(job.Project))
            {
                if (job.Project.Length > 50)
                    _messageBuilder.AppendLine("Project name cannot be longer than 50 characters.");

                if (ContainsIllegalCharacters(job.Project))
                    _messageBuilder.AppendLine("Project name contains illegal characters.");
            }

            if (job.WaitingLimit < 0)
                _messageBuilder.AppendLine("Waiting limit cannot be lower than 0.");

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

            if (job.EnvironmentVariables != null)
            {
                foreach (EnvironmentVariable variable in job.EnvironmentVariables)
                {
                    if (string.IsNullOrEmpty(variable.Name))
                        _messageBuilder.AppendLine($"Environment variable's name for the job cannot be empty. ({variable.Name}={variable.Value})");
                }
            }

            if (job.Tasks == null || job.Tasks.Count == 0)
            {
                _messageBuilder.AppendLine("Each job has to contain at least one task.");
            }
            else
            {
                job.Tasks.ForEach(task => ValidateTaskSpecificationInput(task));
            }
        
            return _messageBuilder.ToString();
        }
        private void ValidateTaskSpecificationInput(TaskSpecification task)
        {
            if (task.CommandTemplateId <= 0) 
                _messageBuilder.AppendLine("CommandTemplateId cannot be empty or <= 0.");

            if (string.IsNullOrEmpty(task.Name))
                _messageBuilder.AppendLine("Task name cannot be empty.");

            if (task.Name.Length > 50)
                _messageBuilder.AppendLine($"Task name \"{task.Name}\" cannot be longer than 50 characters.");

            if (ContainsIllegalCharacters(task.Name))
                _messageBuilder.AppendLine("Task name contains illegal characters.");

            if (task.MinCores <= 0)
                _messageBuilder.AppendLine($"Minimal number of cores for task \"{task.Name}\" has to be greater than 0.");

            if (task.MaxCores <= 0)
                _messageBuilder.AppendLine($"Maximal number of cores for task \"{task.Name}\" has to be greater than 0.");

            if (task.MinCores > task.MaxCores)
                _messageBuilder.AppendLine($"Minimal number of cores for task \"{task.Name}\" cannot be greater than maximal number of cores.");

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
                        _messageBuilder.AppendLine($"JobArrays specification for task \"{task.Name}\" cannot be longer than 40 characters.");

                    task.JobArrays = task.JobArrays.Replace(" ", string.Empty);
                    var splittedArray = task.JobArrays.Split(':');
                    if (splittedArray.Length >= 1)
                    {
                        int step = 1;
                        var interval = splittedArray[0].Split('-');
                        if ((splittedArray.Length == 1 || (splittedArray.Length == 2 && int.TryParse(splittedArray[1], out step)))
                            && interval.Length == 2 && int.TryParse(interval[0], out int min) && int.TryParse(interval[1], out int max))
                        {
                            if (!(min < max && min + step <= max))
                            {
                                _messageBuilder.AppendLine($"JobArrays parameter for task \"{task.Name}\" has wrong filled minimum or maximum or step value.");
                            }
                        }
                        else
                        {
                            _messageBuilder.AppendLine($"JobArrays parameter for task \"{task.Name}\" has wrong definition.");
                        }
                    }
                }
            }

            if (task.TaskParalizationSpecifications?.Sum(s => s.MaxCores) > task.MaxCores)
                _messageBuilder.AppendLine($"TaskParalizationSpecifications count of maximal cores for task \"{task.Name}\" must be lower or equals to Maximal number of cores in task.");

            if (!string.IsNullOrEmpty(task.PlacementPolicy))
            {
                if (task.PlacementPolicy.Length > 40)
                    _messageBuilder.AppendLine($"Placement policy specification for task \"{task.Name}\" cannot be longer than 40 characters.");

                if (ContainsIllegalCharacters(task.PlacementPolicy))
                    _messageBuilder.AppendLine($"Placement policy specification for task \"{task.Name}\" contains illegal characters.");
            }

            if (!string.IsNullOrEmpty(task.StandardInputFile))
            {
                if (task.StandardInputFile.Length > 30)
                    _messageBuilder.AppendLine($"Standard input file for task \"{task.Name}\" cannot be longer than 30 characters.");

                if (ContainsIllegalCharacters(task.StandardInputFile))
                    _messageBuilder.AppendLine($"Standard input file for task \"{task.Name}\" contains illegal characters.");
            }

            if (!string.IsNullOrEmpty(task.StandardOutputFile))
            {
                if (task.StandardOutputFile.Length > 30)
                    _messageBuilder.AppendLine($"Standard output file for task \"{task.Name}\" cannot be longer than 30 characters.");

                if (ContainsIllegalCharacters(task.StandardOutputFile))
                    _messageBuilder.AppendLine($"Standard output file for task \"{task.Name}\" contains illegal characters.");
            }

            if (!string.IsNullOrEmpty(task.StandardErrorFile))
            {
                if (task.StandardErrorFile.Length > 30)
                    _messageBuilder.AppendLine($"Standard error file for task \"{task.Name}\" cannot be longer than 30 characters.");

                if (ContainsIllegalCharacters(task.StandardErrorFile))
                    _messageBuilder.AppendLine($"Standard error file for task \"{task.Name}\" contains illegal characters.");
            }

            if (!string.IsNullOrEmpty(task.ProgressFile.RelativePath))
            {
                if (task.ProgressFile.RelativePath.Length > 50)
                    _messageBuilder.AppendLine($"Progress file for task \"{task.Name}\" cannot be longer than 50 characters.");

                if (ContainsIllegalCharactersForPath(task.ProgressFile.RelativePath))
                    _messageBuilder.AppendLine($"Progress file for task \"{task.Name}\" contains illegal characters.");
            }

            if (!string.IsNullOrEmpty(task.LogFile.RelativePath))
            {
                if (task.LogFile.RelativePath.Length > 50)
                    _messageBuilder.AppendLine($"Log file for task \"{task.Name}\" cannot be longer than 50 characters.");
                if (ContainsIllegalCharactersForPath(task.LogFile.RelativePath))
                    _messageBuilder.AppendLine($"Log file for task \"{task.Name}\" contains illegal characters.");
            }

            if (task.RequiredNodes != null)
            {
                foreach (TaskSpecificationRequiredNode requiredNode in task.RequiredNodes)
                {
                    if (requiredNode.NodeName.Length > 40)
                        _messageBuilder.AppendLine($"Required node \"{requiredNode.NodeName}\" specification for task \"{task.Name}\" cannot be longer than 40 characters.");

                    if (ContainsIllegalCharacters(requiredNode.NodeName))
                        _messageBuilder.AppendLine($"Required node \"{requiredNode.NodeName}\" specification for task \"{task.Name}\" contains illegal characters.");
                }
            }

            if (task.EnvironmentVariables != null)
            {
                foreach (EnvironmentVariable variable in task.EnvironmentVariables)
                {
                    if (string.IsNullOrEmpty(variable.Name))
                        _messageBuilder.AppendLine($"Environment variable's name for task \"{task.Name}\" cannot be empty. ({variable.Name} = {variable.Value})");
                }
            }
        }
        #endregion
    }
}

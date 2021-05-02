using HEAppE.DomainObjects.JobManagement;
using HEAppE.Utils.Validation;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.BusinessLogicTier.Logic.JobManagement.Validators
{
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
            //Task Validation
            job.Tasks.ForEach(task => ValidateTaskSpecificationInput(task));

            //Task Dependency
            for (int i = 0; i < job.Tasks.Count; i++)
            {
                //Validation cluster in tasks
                if (job.Tasks[i].CommandTemplate.ClusterNodeType.Cluster.Id != job.Cluster.Id)
                {
                    _messageBuilder.AppendLine($"Task \"{job.Tasks[i].Name}\" must used same HPC Cluster as job " +
                                               $"\"{job.Name}\".");
                }

                if (job.FileTransferMethodId != job.Tasks[i].CommandTemplate.ClusterNodeType.FileTransferMethodId)
                {
                    _messageBuilder.AppendLine($"Command template \"{job.Tasks[i].CommandTemplate.Id}\" for task " +
                                               $"\"{job.Tasks[i].Name}\" has different file transfer method " +
                                               $"\"{ job.Tasks[i].CommandTemplate.ClusterNodeType.FileTransferMethodId}\" " +
                                               $"than job file transfer method \"{job.FileTransferMethodId}\".");
                }

                if (job.Tasks[i].CommandTemplate.ClusterNodeType.Id != job.Tasks[i].ClusterNodeTypeId)
                {
                    _messageBuilder.AppendLine($"Task \"{job.Tasks[i].Name}\" must used same ClusterNodeTypeId " +
                                               $"\"{job.Tasks[i].ClusterNodeTypeId}\" which is defined in CommandTemplate " +
                                               $"(ClusterNodeTypeId=\"{job.Tasks[i].CommandTemplate.ClusterNodeType.Id}\").");
                }


                if (job.Tasks[i].DependsOn != null && job.Tasks[i].DependsOn.Count > 0)
                {
                    List<TaskSpecification> prevTasks = new List<TaskSpecification>();
                    foreach (var dependTask in job.Tasks[i].DependsOn)
                    {
                        if (dependTask.TaskSpecification == dependTask.ParentTaskSpecification)
                        {
                            //Inself reference
                            _messageBuilder.AppendLine($"Depending task \"{dependTask.TaskSpecification.Name}\" for task " +
                                                       $"\"{job.Tasks[i].Name}\" references inself.");
                        }

                        var prevTask = prevTasks.Where(w => ReferenceEquals(w, dependTask.ParentTaskSpecification)).FirstOrDefault();
                        if (prevTask is null)
                        {
                            prevTasks.Add(dependTask.ParentTaskSpecification);
                        }
                        else
                        {
                            //Same dependency
                            _messageBuilder.AppendLine($"Depending task \"{dependTask.ParentTaskSpecification.Name}\" for task " +
                                                       $"\"{job.Tasks[i].Name}\" twice same reference.");
                        }

                        bool previousTask = false;
                        for (int j = (i - 1); j >= 0; j--)
                        {
                            if (dependTask.ParentTaskSpecification == job.Tasks[j])
                            {
                                previousTask = true;
                            }
                        }

                        if (!previousTask)
                        {
                            //Circular dependency
                            _messageBuilder.AppendLine(
                                $"Depending task \"{dependTask.ParentTaskSpecification.Name}\" for task \"{job.Tasks[i].Name}\" " +
                                $"can reference only on previous task.");
                        }
                    }
                }
            }
            return _messageBuilder.ToString();
        }
        private void ValidateTaskSpecificationInput(TaskSpecification task)
        {
            foreach (CommandTemplateParameter parameter in task.CommandTemplate.TemplateParameters)
            {
                if (string.IsNullOrEmpty(parameter.Query) &&
                    (task.CommandParameterValues == null || !task.CommandParameterValues.Where(w => w.TemplateParameter == parameter).Any()))
                {
                    _messageBuilder.AppendLine($"Command Template parameter \"{parameter.Identifier}\" does not have a value.");
                }
            }

            if (task.ClusterNodeTypeId != task.CommandTemplate.ClusterNodeTypeId)
            {
                _messageBuilder.AppendLine($"Task {task.Name} has wrong CommandTemplate");
            }
        }
        #endregion
    }
}

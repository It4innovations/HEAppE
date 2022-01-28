using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.ConversionAdapter;
using HEAppE.MiddlewareUtils;
using log4net;

namespace HEAppE.HpcConnectionFramework
{
    public abstract class SchedulerDataConvertor : ISchedulerDataConvertor
    {
        #region Instances

        protected ConversionAdapterFactory _conversionAdapterFactory;

        protected static ILog _log;
        #endregion
        #region Constructors

        public SchedulerDataConvertor(ConversionAdapterFactory conversionAdapterFactory)
        {
            _conversionAdapterFactory = conversionAdapterFactory;
            _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }
        #endregion
        #region ISchedulerDataConvertor Members

        public virtual object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd)
        {
            ISchedulerJobAdapter jobAdapter = _conversionAdapterFactory.CreateJobAdapter();
            jobAdapter.SetNotifications(jobSpecification.NotificationEmail,jobSpecification.NotifyOnStart, jobSpecification.NotifyOnFinish, jobSpecification.NotifyOnAbort);

            // Setting global parameters for all tasks
            var globalJobParameters = (string)jobAdapter.AllocationCmd;
            var tasks = new List<object>();
            if (jobSpecification.Tasks is not null && jobSpecification.Tasks.Any())
            {
                foreach (var task in jobSpecification.Tasks)
                {
                    tasks.Add($"_{task.Id}=$({(string)ConvertTaskSpecificationToTask(jobSpecification, task, schedulerAllocationCmd)}{globalJobParameters});echo $_{task.Id};");
                }
            }

            jobAdapter.SetTasks(tasks);
            return jobAdapter.AllocationCmd;
        }

        public object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object schedulerAllocationCmd)
        {
            ISchedulerTaskAdapter taskAdapter = _conversionAdapterFactory.CreateTaskAdapter(schedulerAllocationCmd);
            taskAdapter.DependsOn = taskSpecification.DependsOn;
            taskAdapter.SetEnvironmentVariablesToTask(taskSpecification.EnvironmentVariables);
            taskAdapter.IsExclusive = taskSpecification.IsExclusive;
            taskAdapter.SetRequestedResourceNumber(taskSpecification.ClusterNodeType.RequestedNodeGroups.Select(s => s.Name).ToList(),
                                                   taskSpecification.RequiredNodes.Select(s => s.NodeName).ToList(),
                                                   taskSpecification.PlacementPolicy,
                                                   taskSpecification.TaskParalizationSpecifications,
                                                   Convert.ToInt32(taskSpecification.MinCores),
                                                   Convert.ToInt32(taskSpecification.MaxCores),
                                                   taskSpecification.ClusterNodeType.CoresPerNode);

            // Do not change!!! Task name on the cluster is set as ID of the used task specification to enable pairing of cluster task info with DB task info.
            taskAdapter.Name = taskSpecification.Id.ToString(CultureInfo.InvariantCulture);

            if (Convert.ToInt32(taskSpecification.WalltimeLimit) > 0)
            {
                taskAdapter.Runtime = Convert.ToInt32(taskSpecification.WalltimeLimit);
            }

            string taskClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.FileTransferMethod.Cluster.LocalBasepath, jobSpecification);
            string workDirectory = FileSystemUtils.GetTaskClusterDirectoryPath(taskClusterDirectory, taskSpecification);

            string stdErrFilePath = FileSystemUtils.ConcatenatePaths(workDirectory, taskSpecification.StandardErrorFile);
            taskAdapter.StdErrFilePath = workDirectory.Equals(stdErrFilePath) ? string.Empty : stdErrFilePath;

            string stdInFilePath = FileSystemUtils.ConcatenatePaths(workDirectory, taskSpecification.StandardInputFile);
            taskAdapter.StdInFilePath = workDirectory.Equals(stdInFilePath) ? string.Empty : stdInFilePath;

            string stdOutFilePath = FileSystemUtils.ConcatenatePaths(workDirectory, taskSpecification.StandardOutputFile);
            taskAdapter.StdOutFilePath = workDirectory.Equals(stdOutFilePath) ? string.Empty : stdOutFilePath;

            taskAdapter.WorkDirectory = workDirectory;
            taskAdapter.JobArrays = taskSpecification.JobArrays;
            taskAdapter.IsRerunnable = !string.IsNullOrEmpty(taskSpecification.JobArrays) || taskSpecification.IsRerunnable;

            taskAdapter.Queue = taskSpecification.ClusterNodeType.Queue;
            taskAdapter.CpuHyperThreading = taskSpecification.CpuHyperThreading ?? false;

            CommandTemplate template = taskSpecification.CommandTemplate;
            if (template != null)
            {
                Dictionary<string, string> templateParameters = CreateTemplateParameterValuesDictionary(jobSpecification, taskSpecification,
                                                                template.TemplateParameters, taskSpecification.CommandParameterValues);
                taskAdapter.SetPreparationAndCommand(workDirectory,
                                                     ReplaceTemplateDirectivesInCommand(template.PreparationScript, templateParameters),
                                                     CreateCommandLineForTask(template, taskSpecification, jobSpecification, templateParameters),
                                                     stdOutFilePath, stdErrFilePath, CreateTaskDirectorySymlinkCommand(taskSpecification));
            }
            else
            {
                throw new ApplicationException(@$"Command Template ""{taskSpecification.CommandTemplate.Name}"" for task 
                                                  ""{taskSpecification.Name}"" does not exist in the adaptor configuration.");
            }
            return taskAdapter.AllocationCmd;
        }
        #region Abstract Members

        public abstract IEnumerable<string> GetJobIds(string responseMessage);

        public abstract SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage);

        public abstract IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object responseMessage);
        #endregion
        #endregion
        #region Local Methods
        protected virtual string CreateCommandLineForTask(CommandTemplate template, TaskSpecification taskSpecification,
                JobSpecification jobSpecification, Dictionary<string, string> templateParameters)
        {
            return CreateCommandLineForTemplate(template, templateParameters);
        }

        protected virtual string CreateCommandLineForTemplate(CommandTemplate template, Dictionary<string, string> templateParameters)
        {
            string commandParameters = template.CommandParameters;
            string commandLine = template.ExecutableFile + " " + commandParameters;
            return ReplaceTemplateDirectivesInCommand(commandLine, templateParameters);
        }

        public static Dictionary<string, string> CreateTemplateParameterValuesDictionary(JobSpecification jobSpecification, TaskSpecification taskSpecification,
                ICollection<CommandTemplateParameter> templateParameters, ICollection<CommandTemplateParameterValue> taskParametersValues)
        {
            Dictionary<string, string> finalParameters = new Dictionary<string, string>();
            foreach (CommandTemplateParameter templateParameter in templateParameters)
            {
                CommandTemplateParameterValue taskParametersValue = (from parameterValue in taskParametersValues
                                                                     where parameterValue.TemplateParameter.Identifier == templateParameter.Identifier
                                                                     select parameterValue).FirstOrDefault();
                if (taskParametersValue != null)
                {
                    // If taskParametersValue represent already escaped string of generic key-value pairs, don't escape it again.
                    var isStringOfGenericParameters = templateParameter.CommandTemplate.IsGeneric && Regex.IsMatch(taskParametersValue.Value, "\".+\"");
                    finalParameters.Add(templateParameter.Identifier, isStringOfGenericParameters ? taskParametersValue.Value : Regex.Escape(taskParametersValue.Value));
                }
                else
                {
                    finalParameters.Add(templateParameter.Identifier, GetTemplateParameterValueFromQuery(jobSpecification, taskSpecification, templateParameter.Query));
                }
            }
            return finalParameters;
        }

        private static string GetTemplateParameterValueFromQuery(JobSpecification jobSpecification, TaskSpecification taskSpecification, string parameterQuery)
        {
            if (parameterQuery.StartsWith("Job."))
            {
                return GetPropertyValueForQuery(jobSpecification, parameterQuery);
            }
            else if (parameterQuery == "Task.Workdir")
            {
                string taskClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.FileTransferMethod.Cluster.LocalBasepath, jobSpecification);
                return FileSystemUtils.GetTaskClusterDirectoryPath(taskClusterDirectory, taskSpecification);
            }
            else if (parameterQuery.StartsWith("Task."))
            {
                return GetPropertyValueForQuery(taskSpecification, parameterQuery);
            }
            return parameterQuery;
        }


        private static string GetPropertyValueForQuery(object objectForQuery, string query)
        {
            PropertyInfo property = objectForQuery.GetType().GetProperty(GetPropertyNameFromQuery(query));
            if (property != null)
            {
                object propertyValue = property.GetValue(objectForQuery, null);
                return propertyValue != null ? propertyValue.ToString() : string.Empty;
            }
            return null;
        }

        protected string CreateTaskDirectorySymlinkCommand(TaskSpecification taskSpecification)
        {
            string symlinkCommand = "";
            if (taskSpecification.DependsOn.Count > 0)
            {
                long dependsOnIdLast = taskSpecification.DependsOn.Max(x => x.ParentTaskSpecificationId);
                var dependsOn = taskSpecification.DependsOn
                    .Where(x => x.ParentTaskSpecificationId == dependsOnIdLast).First();
                if (dependsOn != null)
                    symlinkCommand = $"ln -s ../{dependsOnIdLast}/* .";
            }
            return symlinkCommand;
        }

        private static string GetPropertyNameFromQuery(string parameterQuery)
        {
            return parameterQuery.Substring(parameterQuery.IndexOf('.') + 1);
        }

        public string ReplaceTemplateDirectivesInCommand(string commandLine, Dictionary<string, string> templateParameters)
        {
            if (commandLine == null)
                return null;
            string replacedCommandLine = Regex.Replace(commandLine, @"%%\{([\w\.]+)\}", delegate (Match match)
            {
                string parameterIdentifier = match.Groups[1].Value;
                if (templateParameters != null && templateParameters.ContainsKey(parameterIdentifier))
                {
                    return templateParameters[parameterIdentifier];
                }
                throw new ApplicationException("Parameter \"" + parameterIdentifier + "\" in the command template \"" + commandLine +
                                               "\" could not be found either as a property of the task, nor as an additional parameter.");
            });
            return replacedCommandLine;
        }
        #endregion


    }
}
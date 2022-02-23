using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.MiddlewareUtils;
using HEAppE.Utils;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters
{
    public abstract class SchedulerDataConvertor : ISchedulerDataConvertor
    {
        #region Instances
        protected ConversionAdapterFactory _conversionAdapterFactory;

        protected readonly ILog _log;
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
            jobAdapter.SetNotifications(jobSpecification.NotificationEmail, jobSpecification.NotifyOnStart, jobSpecification.NotifyOnFinish, jobSpecification.NotifyOnAbort);
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

        public virtual object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object schedulerAllocationCmd)
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

            string jobClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.FileTransferMethod.Cluster.LocalBasepath, jobSpecification);
            string workDirectory = FileSystemUtils.GetTaskClusterDirectoryPath(jobClusterDirectory, taskSpecification);

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
            taskAdapter.ClusterAllocationName = taskSpecification.ClusterNodeType.ClusterAllocationName;
            taskAdapter.CpuHyperThreading = taskSpecification.CpuHyperThreading ?? false;

            CommandTemplate template = taskSpecification.CommandTemplate;
            if (template is null)
            {
                throw new ApplicationException(@$"Command Template ""{taskSpecification.CommandTemplate.Name}"" for task 
                                                  ""{taskSpecification.Name}"" does not exist in the adaptor configuration.");
            }

            Dictionary<string, string> templateParameters = CreateTemplateParameterValuesDictionary(jobSpecification, taskSpecification,
                                                            template.TemplateParameters, taskSpecification.CommandParameterValues);
            taskAdapter.SetPreparationAndCommand(workDirectory, ReplaceTemplateDirectivesInCommand(template.PreparationScript, templateParameters),
                                                 ReplaceTemplateDirectivesInCommand($"{template.ExecutableFile} {template.CommandParameters}", templateParameters),
                                                 stdOutFilePath, stdErrFilePath, CreateTaskDirectorySymlinkCommand(taskSpecification));

            return taskAdapter.AllocationCmd;
        }

        public void FillingSchedulerJobResultObjectFromSchedulerAttribute(Cluster cluster, object schedulerResultObj, Dictionary<string, string> parsedParameters)
        {
            var properties = schedulerResultObj.GetType()
                                                .GetProperties();

            foreach (var property in properties)
            {
                var schedulerPropertyNames = property.GetCustomAttributes(typeof(SchedulerAttribute), true)
                                                      .Cast<SchedulerAttribute>()
                                                      .FirstOrDefault()?.Names ?? Enumerable.Empty<string>();
                foreach (var schedulerPropertyName in schedulerPropertyNames)
                {
                    if (parsedParameters.ContainsKey(schedulerPropertyName))
                    {
                        if (property.CanWrite)
                        {
                            var value = ChangeType(cluster, parsedParameters[schedulerPropertyName], property.PropertyType);
                            property.SetValue(schedulerResultObj, value, null);
                        }
                        break;
                    }
                }
            }
        }
        #region Abstract Members
        public abstract ClusterNodeUsage ReadQueueActualInformation(object responseMessage, ClusterNodeType nodeType);
        public abstract IEnumerable<string> GetJobIds(string responseMessage);
        public abstract SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo);
        public abstract IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage);
        #endregion
        #endregion
        #region Local Methods
        protected static Dictionary<string, string> CreateTemplateParameterValuesDictionary(JobSpecification jobSpecification, TaskSpecification taskSpecification,
                ICollection<CommandTemplateParameter> templateParameters, ICollection<CommandTemplateParameterValue> taskParametersValues)
        {
            var finalParameters = new Dictionary<string, string>();
            foreach (CommandTemplateParameter templateParameter in templateParameters)
            {
                var taskParametersValue = taskParametersValues.Where(w => w.TemplateParameter.Identifier == templateParameter.Identifier)
                                                               .FirstOrDefault();
                if (taskParametersValue is not null)
                {
                    // If taskParametersValue represent already escaped string of generic key-value pairs, don't escape it again.
                    var isStringOfGenericParameters = templateParameter.CommandTemplate.IsGeneric && Regex.IsMatch(taskParametersValue.Value, @""".+""", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    finalParameters.Add(templateParameter.Identifier, isStringOfGenericParameters ? taskParametersValue.Value : Regex.Escape(taskParametersValue.Value));
                }
                else
                {
                    string templateParameterValueFromQuery = templateParameter.Query;
                    if (templateParameter.Query.StartsWith("Job."))
                    {
                        templateParameterValueFromQuery = GetPropertyValueForQuery(jobSpecification, templateParameter.Query);
                    }

                    if (templateParameter.Query == "Task.Workdir")
                    {
                        string taskClusterDirectory = FileSystemUtils.GetJobClusterDirectoryPath(jobSpecification.FileTransferMethod.Cluster.LocalBasepath, jobSpecification);
                        templateParameterValueFromQuery = FileSystemUtils.GetTaskClusterDirectoryPath(taskClusterDirectory, taskSpecification);
                    }

                    if (templateParameter.Query.StartsWith("Task."))
                    {
                        templateParameterValueFromQuery = GetPropertyValueForQuery(taskSpecification, templateParameter.Query);
                    }
                    finalParameters.Add(templateParameter.Identifier, templateParameterValueFromQuery);
                }
            }
            return finalParameters;
        }

        protected static string ReplaceTemplateDirectivesInCommand(string commandLine, Dictionary<string, string> templateParameters)
        {
            if (string.IsNullOrEmpty(commandLine))
            {
                return null;
            }

            string replacedCommandLine = Regex.Replace(commandLine, @"%%\{([\w\.]+)\}", delegate (Match match)
            {
                string parameterIdentifier = match.Groups[1].Value;
                if (templateParameters != null && templateParameters.ContainsKey(parameterIdentifier))
                {
                    return templateParameters[parameterIdentifier];
                }
                throw new ApplicationException(@$"Parameter ""{parameterIdentifier}"" in the command template ""{commandLine}"" 
                                                could not be found either as a property of the task, nor as an additional parameter.");
            });
            return replacedCommandLine;
        }

        protected static string CreateTaskDirectorySymlinkCommand(TaskSpecification taskSpecification)
        {
            string symlinkCommand = string.Empty;
            if (taskSpecification.DependsOn.Any())
            {
                long dependsOnIdLast = taskSpecification.DependsOn.Max(x => x.ParentTaskSpecificationId);
                return taskSpecification.DependsOn.FirstOrDefault(x => x.ParentTaskSpecificationId == dependsOnIdLast) == null
                                                ? symlinkCommand
                                                : symlinkCommand = $"ln -s ../{dependsOnIdLast}/* .";
            }

            return symlinkCommand;
        }

        private static string GetPropertyValueForQuery(object objectForQuery, string query)
        {
            PropertyInfo property = objectForQuery.GetType().GetProperty(query[(query.IndexOf('.') + 1)..]);
            if (property == null)
            {
                return null;
            }

            object propertyValue = property.GetValue(objectForQuery, null);
            return propertyValue != null ? propertyValue.ToString() : string.Empty;

        }

        /// <summary>
        /// Change type from object
        /// </summary>
        /// <param name="cluster">Cluster</param>
        /// <param name="obj">Value for converting</param>
        /// <param name="type">Type for converting</param>
        /// <returns></returns>
        private static object ChangeType(Cluster cluster, object obj, Type type)
        {
            switch (type.Name)
            {
                case "TimeSpan":
                    {
                        string parsedText = Convert.ToString(obj);
                        if (!string.IsNullOrEmpty(parsedText) && TimeSpan.TryParse(parsedText, out TimeSpan timeSpan))
                        {
                            return timeSpan;
                        }
                        else
                        {
                            return new TimeSpan(0);
                        }
                    }
                case "DateTime":
                    {
                        string parsedText = Convert.ToString(obj);
                        if (!string.IsNullOrEmpty(parsedText) && DateTime.TryParse(parsedText, out DateTime date))
                        {
                            return date.Convert(cluster.TimeZone);
                        }
                        else
                        {
                            return null;
                        }
                    }
                default:
                    {
                        return Convert.ChangeType(obj, type);
                    }
            }
        }
        #endregion
    }
}
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
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SchedulerDataConvertor(ConversionAdapterFactory conversionAdapterFactory)
        {
            this.conversionAdapterFactory = conversionAdapterFactory;
        }

        #region ISchedulerDataConvertor Members
        public virtual SubmittedJobInfo ConvertJobToJobInfo(object job)
        {
            SubmittedJobInfo jobInfo = new SubmittedJobInfo();
            ISchedulerJobAdapter jobAdapter = conversionAdapterFactory.CreateJobAdapter(job);
            List<object> allTasks = jobAdapter.GetTaskList();
            jobInfo.Tasks = ConvertAllTasksToTaskInfos(allTasks);
            jobInfo.Name = jobAdapter.Name;
            jobInfo.Project = jobAdapter.Project;
            jobInfo.State = jobAdapter.State;
            jobInfo.CreationTime = jobAdapter.CreateTime;
            jobInfo.SubmitTime = jobAdapter.SubmitTime;
            jobInfo.StartTime = jobAdapter.StartTime;
            jobInfo.EndTime = jobAdapter.EndTime;
            jobInfo.TotalAllocatedTime = CountTotalAllocatedTime(jobInfo.Tasks);
            return jobInfo;
        }

        public virtual SubmittedTaskInfo ConvertTaskToTaskInfo(object task)
        {
            SubmittedTaskInfo taskInfo = new SubmittedTaskInfo();
            ISchedulerTaskAdapter taskAdapter = conversionAdapterFactory.CreateTaskAdapter(task);
            taskInfo.TaskAllocationNodes = taskAdapter.AllocatedCoreIds?
                                                        .Select(s => new SubmittedTaskAllocationNodeInfo()
                                                        {
                                                            AllocationNodeId = s,
                                                        }).ToList();
            taskInfo.Name = taskAdapter.Name;
            taskInfo.State = taskAdapter.State;
            taskInfo.StartTime = taskAdapter.StartTime;
            taskInfo.EndTime = taskAdapter.EndTime;
            taskInfo.ErrorMessage = taskAdapter.ErrorMessage;
            taskInfo.AllocatedTime = taskAdapter.AllocatedTime;
            taskInfo.AllParameters = StringUtils.ConvertDictionaryToString(taskAdapter.AllParameters);
            return taskInfo;
        }

        public virtual object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object job)
        {
            ISchedulerJobAdapter jobAdapter = conversionAdapterFactory.CreateJobAdapter(job);
            jobAdapter.Name = ConvertJobName(jobSpecification);
            jobAdapter.SetNotifications(jobSpecification.NotificationEmail,
                jobSpecification.NotifyOnStart, jobSpecification.NotifyOnFinish,
                jobSpecification.NotifyOnAbort);
            jobAdapter.Project = jobSpecification.Project;
            jobAdapter.AccountingString = jobSpecification.SubmitterGroup.AccountingString;
            if (Convert.ToInt32(jobSpecification.WalltimeLimit) > 0)
            {
                jobAdapter.Runtime = Convert.ToInt32(jobSpecification.WalltimeLimit);
            }
            jobAdapter.SetTasks(CreateTasks(jobSpecification, jobAdapter));
            log.Debug(jobAdapter.Source);
            return jobAdapter.Source;
        }

        public object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object task)
        {
            ISchedulerTaskAdapter taskAdapter = conversionAdapterFactory.CreateTaskAdapter(task);
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
            if (!string.IsNullOrEmpty(taskSpecification.JobArrays))
                taskAdapter.IsRerunnable = true;
            else
                taskAdapter.IsRerunnable = taskSpecification.IsRerunnable;

            taskAdapter.Queue = taskSpecification.ClusterNodeType.Queue;
            taskAdapter.CpuHyperThreading = taskSpecification.CpuHyperThreading ?? false;

            CommandTemplate template = taskSpecification.CommandTemplate;
            if (template != null)
            {
                Dictionary<string, string> templateParameters =
                    CreateTemplateParameterValuesDictionary(jobSpecification, taskSpecification, template.TemplateParameters, taskSpecification.CommandParameterValues);
                taskAdapter.SetPreparationAndCommand(
                    workDirectory,
                    ReplaceTemplateDirectivesInCommand(template.PreparationScript, templateParameters),
                    CreateCommandLineForTask(template, taskSpecification, jobSpecification, templateParameters),
                    stdOutFilePath, stdErrFilePath, CreateTaskDirectorySymlinkCommand(taskSpecification));
            }
            else
            {
                throw new ApplicationException("Command Template \"" + taskSpecification.CommandTemplate.Name + "\" for task \"" +
                                               taskSpecification.Name + "\" does not exist in the adaptor configuration.");
            }
            return taskAdapter.Source;
        }
        #endregion

        #region Local Methods
        protected virtual List<object> CreateTasks(JobSpecification jobSpecification, ISchedulerJobAdapter jobAdapter)
        {
            List<object> tasks = new List<object>();
            foreach (TaskSpecification taskSpecification in jobSpecification.Tasks)
            {
                object task = jobAdapter.CreateEmptyTaskObject();
                task = ConvertTaskSpecificationToTask(jobSpecification, taskSpecification, task);
                tasks.Add(task);
            }
            return tasks;
        }

        protected virtual List<SubmittedTaskInfo> ConvertAllTasksToTaskInfos(List<object> tasks)
        {
            if (tasks != null)
            {
                List<SubmittedTaskInfo> taskInfos = new List<SubmittedTaskInfo>(tasks.Count);
                foreach (object task in tasks)
                {
                    taskInfos.Add(ConvertTaskToTaskInfo(task));
                }
                return taskInfos;
            }
            return new List<SubmittedTaskInfo>();
        }

        protected virtual double CountTotalAllocatedTime(ICollection<SubmittedTaskInfo> tasks)
        {
            double totalTime = 0;
            if (tasks != null)
            {
                foreach (SubmittedTaskInfo task in tasks)
                {
                    if (task.StartTime.HasValue)
                    {
                        totalTime += task.AllocatedTime ?? 0;
                    }
                }
            }
            return totalTime;
        }

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

        protected virtual string ConvertJobName(JobSpecification jobSpecification)
        {
            return jobSpecification.Name;
        }

        protected virtual string ConvertTaskName(string taskName, JobSpecification jobSpecification)
        {
            return taskName;
        }
        #endregion

        #region Instance Fields
        protected ConversionAdapterFactory conversionAdapterFactory;
        #endregion
    }
}
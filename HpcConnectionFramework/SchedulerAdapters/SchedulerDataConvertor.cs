using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.Exceptions.Internal;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.Utils;
using log4net;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters;

/// <summary>
///     Scheduler data convertor
/// </summary>
public abstract class SchedulerDataConvertor : ISchedulerDataConvertor
{
    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="conversionAdapterFactory">Conversion adapter factory</param>
    public SchedulerDataConvertor(ConversionAdapterFactory conversionAdapterFactory)
    {
        _conversionAdapterFactory = conversionAdapterFactory;
        _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    }

    #endregion

    #region Instances

    /// <summary>
    ///     Conversion factory
    /// </summary>
    protected ConversionAdapterFactory _conversionAdapterFactory;

    /// <summary>
    ///     Script Configuration
    /// </summary>
    protected static readonly ScriptsConfiguration _scripts = HPCConnectionFrameworkConfiguration.ScriptsSettings;

    /// <summary>
    ///     Logger
    /// </summary>
    protected readonly ILog _log;

    #endregion

    #region ISchedulerDataConvertor Members

    /// <summary>
    ///     Convert job specification to job
    /// </summary>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="schedulerAllocationCmd">Scheduler allocation command</param>
    /// <returns></returns>
    public virtual object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd)
    {
        var jobAdapter = _conversionAdapterFactory.CreateJobAdapter();
        jobAdapter.SetNotifications(jobSpecification.NotificationEmail, jobSpecification.NotifyOnStart,
            jobSpecification.NotifyOnFinish, jobSpecification.NotifyOnAbort);
        // Setting global parameters for all tasks
        var globalJobParameters = (string)jobAdapter.AllocationCmd;
        var tasks = new List<object>();
        if (jobSpecification.Tasks is not null && jobSpecification.Tasks.Any())
            foreach (var task in jobSpecification.Tasks)
                tasks.Add(
                    $"_{task.Id}=$({(string)ConvertTaskSpecificationToTask(jobSpecification, task, schedulerAllocationCmd)}{globalJobParameters});echo $_{task.Id};");

        jobAdapter.SetTasks(tasks);
        return jobAdapter.AllocationCmd;
    }

    /// <summary>
    ///     Convert task specification to task
    /// </summary>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="taskSpecification">Task specification</param>
    /// <param name="schedulerAllocationCmd">Scheduler allocation cmd</param>
    /// <returns></returns>
    /// <exception cref="SchedulerException"></exception>
    public virtual object ConvertTaskSpecificationToTask(JobSpecification jobSpecification,
        TaskSpecification taskSpecification, object schedulerAllocationCmd)
    {
        var taskAdapter = _conversionAdapterFactory.CreateTaskAdapter(schedulerAllocationCmd);
        taskAdapter.DependsOn = taskSpecification.DependsOn;
        taskAdapter.SetEnvironmentVariablesToTask(taskSpecification.EnvironmentVariables);
        taskAdapter.IsExclusive = taskSpecification.IsExclusive;
        taskAdapter.SetRequestedResourceNumber(
            taskSpecification.ClusterNodeType.RequestedNodeGroups.Select(s => s.Name).ToList(),
            taskSpecification.RequiredNodes.Select(s => s.NodeName).ToList(),
            taskSpecification.PlacementPolicy,
            taskSpecification.TaskParalizationSpecifications,
            Convert.ToInt32(taskSpecification.MinCores),
            Convert.ToInt32(taskSpecification.MaxCores),
            taskSpecification.ClusterNodeType.CoresPerNode);

        // Do not change!!! Task name on the cluster is set as ID of the used task specification to enable pairing of cluster task info with DB task info.
        taskAdapter.Name = taskSpecification.Id.ToString(CultureInfo.InvariantCulture);
        taskAdapter.Project = taskSpecification.Project.UseAccountingStringForScheduler
            ? taskSpecification.Project.AccountingString
            : null;

        if (Convert.ToInt32(taskSpecification.WalltimeLimit) > 0)
            taskAdapter.Runtime = Convert.ToInt32(taskSpecification.WalltimeLimit);

        var workDirectory = FileSystemUtils.GetTaskClusterDirectoryPath(taskSpecification, _scripts.SubExecutionsPath);

        var stdErrFilePath = FileSystemUtils.ConcatenatePaths(workDirectory, taskSpecification.StandardErrorFile);
        taskAdapter.StdErrFilePath = workDirectory.Equals(stdErrFilePath) ? string.Empty : stdErrFilePath;

        var stdInFilePath = FileSystemUtils.ConcatenatePaths(workDirectory, taskSpecification.StandardInputFile);
        taskAdapter.StdInFilePath = workDirectory.Equals(stdInFilePath) ? string.Empty : stdInFilePath;

        var stdOutFilePath = FileSystemUtils.ConcatenatePaths(workDirectory, taskSpecification.StandardOutputFile);
        taskAdapter.StdOutFilePath = workDirectory.Equals(stdOutFilePath) ? string.Empty : stdOutFilePath;

        taskAdapter.WorkDirectory = workDirectory;
        taskAdapter.JobArrays = taskSpecification.JobArrays;
        taskAdapter.IsRerunnable = !string.IsNullOrEmpty(taskSpecification.JobArrays) || taskSpecification.IsRerunnable;

        taskAdapter.Queue = taskSpecification.ClusterNodeType.Queue;
        taskAdapter.QualityOfService = taskSpecification.ClusterNodeType.QualityOfService;
        taskAdapter.ClusterAllocationName = taskSpecification.ClusterNodeType.ClusterAllocationName;
        taskAdapter.CpuHyperThreading = taskSpecification.CpuHyperThreading ?? false;

        var template = taskSpecification.CommandTemplate ?? throw new SchedulerException("NotExistingCommandTemplate",
            taskSpecification.CommandTemplate.Name, taskSpecification.Name);

        // Extended allocation parameters from command template
        taskAdapter.ExtendedAllocationCommand = template.ExtendedAllocationCommand;


        var templateParameters = CreateTemplateParameterValuesDictionary(jobSpecification, taskSpecification,
            template.TemplateParameters, taskSpecification.CommandParameterValues);
        taskAdapter.SetPreparationAndCommand(workDirectory,
            ReplaceTemplateDirectivesInCommand(template.PreparationScript, templateParameters),
            ReplaceTemplateDirectivesInCommand($"{template.ExecutableFile} {template.CommandParameters}",
                templateParameters),
            stdOutFilePath, stdErrFilePath, CreateTaskDirectorySymlinkCommand(taskSpecification));

        return taskAdapter.AllocationCmd;
    }

    /// <summary>
    ///     Filling scheduler job result object from scheduler attribute
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="schedulerResultObj">Scheduler</param>
    /// <param name="parsedParameters">Parsed parameters</param>
    public void FillingSchedulerJobResultObjectFromSchedulerAttribute(Cluster cluster, object schedulerResultObj,
        Dictionary<string, string> parsedParameters)
    {
        var properties = schedulerResultObj.GetType()
            .GetProperties();

        foreach (var property in properties)
        {
            var schedulerAttribute = property.GetCustomAttributes(typeof(SchedulerAttribute), true)
                .Cast<SchedulerAttribute>()
                .FirstOrDefault();

            foreach (var schedulerPropertyName in schedulerAttribute?.Names ?? Enumerable.Empty<string>())
                if (parsedParameters.ContainsKey(schedulerPropertyName))
                {
                    if (property.CanWrite)
                    {
                        var value = ChangeType(cluster, parsedParameters[schedulerPropertyName], property.PropertyType,
                            schedulerAttribute?.Format);
                        property.SetValue(schedulerResultObj, value, null);
                    }

                    break;
                }
        }
    }

    #region Abstract Members

    /// <summary>
    ///     Read queue actual information
    /// </summary>
    /// <param name="nodeType">Node type</param>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    public abstract ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage);

    /// <summary>
    ///     Read job parameters from scheduler
    /// </summary>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    public abstract IEnumerable<string> GetJobIds(string responseMessage);

    /// <summary>
    ///     Convert HPC task information from IScheduler job information object
    /// </summary>
    /// <param name="jobInfo">Scheduler job information</param>
    /// <returns></returns>
    public abstract SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo);

    /// <summary>
    ///     Read job parameters from scheduler
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    public abstract IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage);

    #endregion

    #endregion

    #region Local Methods

    /// <summary>
    ///     Create template parameter values dictionary
    /// </summary>
    /// <param name="jobSpecification">Job specification</param>
    /// <param name="taskSpecification">Task specification</param>
    /// <param name="templateParameters">Template parameters</param>
    /// <param name="taskParametersValues">Task parameters values</param>
    /// <returns></returns>
    protected static Dictionary<string, string> CreateTemplateParameterValuesDictionary(
        JobSpecification jobSpecification, TaskSpecification taskSpecification,
        ICollection<CommandTemplateParameter> templateParameters,
        ICollection<CommandTemplateParameterValue> taskParametersValues)
    {
        var finalParameters = new Dictionary<string, string>();
        foreach (var templateParameter in templateParameters.Where(x => x.IsEnabled))
        {
            var taskParametersValue = taskParametersValues
                .Where(w => w.TemplateParameter.Identifier == templateParameter.Identifier)
                .FirstOrDefault();
            if (taskParametersValue is not null)
            {
                // If taskParametersValue represent already escaped string of generic key-value pairs, don't escape it again.
                var isStringOfGenericParameters = templateParameter.CommandTemplate.IsGeneric &&
                                                  Regex.IsMatch(taskParametersValue.Value, @""".+""",
                                                      RegexOptions.IgnoreCase | RegexOptions.Compiled);
                finalParameters.Add(templateParameter.Identifier,
                    isStringOfGenericParameters ? taskParametersValue.Value : Regex.Escape(taskParametersValue.Value));
            }
            else
            {
                var templateParameterValueFromQuery = templateParameter.Query;
                if (templateParameter.Query.StartsWith("Job."))
                    templateParameterValueFromQuery =
                        GetPropertyValueForQuery(jobSpecification, templateParameter.Query);

                if (templateParameter.Query == "Task.Workdir")
                    templateParameterValueFromQuery =
                        FileSystemUtils.GetTaskClusterDirectoryPath(taskSpecification, _scripts.SubExecutionsPath);

                if (templateParameter.Query.StartsWith("Task."))
                    templateParameterValueFromQuery =
                        GetPropertyValueForQuery(taskSpecification, templateParameter.Query);
                finalParameters.Add(templateParameter.Identifier, templateParameterValueFromQuery);
            }
        }

        return finalParameters;
    }

    /// <summary>
    ///     Replace template directives in Command
    /// </summary>
    /// <param name="commandLine">Command line</param>
    /// <param name="templateParameters">Template parameters</param>
    /// <returns></returns>
    /// <exception cref="SchedulerException"></exception>
    protected static string ReplaceTemplateDirectivesInCommand(string commandLine,
        Dictionary<string, string> templateParameters)
    {
        if (string.IsNullOrEmpty(commandLine)) return null;

        var replacedCommandLine = Regex.Replace(commandLine, @"%%\{([\w\.]+)\}", delegate(Match match)
        {
            var parameterIdentifier = match.Groups[1].Value;
            if (templateParameters != null && templateParameters.ContainsKey(parameterIdentifier))
                return templateParameters[parameterIdentifier];
            throw new SchedulerException("NotValidCommandTemplateParameter", parameterIdentifier, commandLine);
        });
        return replacedCommandLine;
    }

    /// <summary>
    ///     Create task directory sym link
    /// </summary>
    /// <param name="taskSpecification">Task specification</param>
    /// <returns></returns>
    protected static string CreateTaskDirectorySymlinkCommand(TaskSpecification taskSpecification)
    {
        var symlinkCommand = string.Empty;
        if (taskSpecification.DependsOn.Any())
        {
            var dependsOnIdLast = taskSpecification.DependsOn.Max(x => x.ParentTaskSpecificationId);
            return taskSpecification.DependsOn.FirstOrDefault(x => x.ParentTaskSpecificationId == dependsOnIdLast) ==
                   null
                ? symlinkCommand
                : symlinkCommand = $"ln -s ../{dependsOnIdLast}/* .";
        }

        return symlinkCommand;
    }

    /// <summary>
    ///     Get property value for query
    /// </summary>
    /// <param name="objectForQuery">Query object</param>
    /// <param name="query">Query</param>
    /// <returns></returns>
    private static string GetPropertyValueForQuery(object objectForQuery, string query)
    {
        var property = objectForQuery.GetType().GetProperty(query[(query.IndexOf('.') + 1)..]);
        if (property == null) return null;

        var propertyValue = property.GetValue(objectForQuery, null);
        return propertyValue != null ? propertyValue.ToString() : string.Empty;
    }

    /// <summary>
    ///     Change type from object
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="obj">Value for converting</param>
    /// <param name="type">Type for converting</param>
    /// <param name="format">Format for converting</param>
    /// <returns></returns>
    private object ChangeType(Cluster cluster, object obj, Type type, string format)
    {
        try
        {
            type = Nullable.GetUnderlyingType(type) is null ? type : Nullable.GetUnderlyingType(type);
            switch (type.Name)
            {
                case "Boolean":
                {
                    var parsedText = Convert.ToString(obj);
                    if (string.IsNullOrEmpty(parsedText)) return null;

                    return int.TryParse(parsedText, out var value)
                        ? Convert.ChangeType(value, type)
                        : Convert.ChangeType(parsedText, type);
                }
                case "TimeSpan":
                {
                    var parsedText = Convert.ToString(obj);
                    if (string.IsNullOrEmpty(parsedText)) return null;

                    if (TimeSpan.TryParse(parsedText, out var timeSpan)) return timeSpan;

                    return default;
                }
                case "DateTime":
                {
                    var parsedText = Convert.ToString(obj)?.Replace("  ", " ");

                    if (string.IsNullOrEmpty(parsedText)) return null;

                    if (string.IsNullOrEmpty(format) && DateTime.TryParse(parsedText, out var date))
                    {
                        return date.Convert(cluster.TimeZone);
                    }

                    date = DateTime.ParseExact(parsedText, format, CultureInfo.InvariantCulture);
                    return date.Convert(cluster.TimeZone);
                }
                default:
                {
                    return obj is null ? null : Convert.ChangeType(obj, type);
                }
            }
        }
        catch
        {
            throw new SchedulerException("ConvertingError", type, obj, format);
        }
    }

    #endregion
}
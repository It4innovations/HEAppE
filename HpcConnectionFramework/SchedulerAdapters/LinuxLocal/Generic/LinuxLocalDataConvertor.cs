using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.Configuration;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.DTO;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Generic.LinuxLocal;

/// <summary>
///     Local Linux HPC Data Convertor
/// </summary>
public class LinuxLocalDataConvertor : SchedulerDataConvertor
{
    #region Instances

    /// <summary>
    ///     Command
    /// </summary>
    protected readonly LinuxLocalCommandScriptPathConfiguration _linuxLocalCommandScripts =
        HPCConnectionFrameworkConfiguration.ScriptsSettings.LinuxLocalCommandScriptPathSettings;

    #endregion

    #region Constructors

    /// <summary>
    ///     Constructor
    /// </summary>
    public LinuxLocalDataConvertor() : base(null)
    {
    }

    #endregion

    #region Local Methods

    /// <summary>
    ///     Convert Tasks To TaskInfoCollection
    /// </summary>
    /// <param name="jobInfo">Job Clutser DTO</param>
    /// <param name="allTasks">Task mapped to LinuxLocalJobDTO Collection</param>
    /// <returns></returns>
    private List<SubmittedTaskInfo> ConvertTasksToTaskInfoCollection(LinuxLocalInfo jobInfo,
        List<LinuxLocalJobDTO> allTasks)
    {
        List<SubmittedTaskInfo> taskCollection = new();

        foreach (var taskAdapter in allTasks)
        {
            taskAdapter.CreationTime = jobInfo.CreateTime;
            taskAdapter.SubmitTime = jobInfo.SubmitTime ?? default;
            taskCollection.Add(ConvertTaskToTaskInfo(taskAdapter));
        }

        return taskCollection;
    }

    #endregion

    #region SchedulerDataConvertor Members

    /// <summary>
    ///     Read actual queue status from scheduler
    /// </summary>
    /// <param name="nodeType">Cluster node type</param>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    public override ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Get Job IDs (Cluster Jobs)
    /// </summary>
    /// <param name="responseMessage">Scheduler response message</param>
    /// <returns></returns>
    public override IEnumerable<string> GetJobIds(string responseMessage)
    {
        List<string> jobIds = new();
        var jobsAdapter = JsonSerializer.Deserialize<LinuxLocalInfo>(responseMessage);
        jobsAdapter.Jobs.ForEach(job => jobIds.Add(job.SchedulerJobId));
        return jobIds;
    }

    /// <summary>
    ///     Convert Task To TaskInfo
    /// </summary>
    /// <param name="jobDTO">Scheduler job information</param>
    /// <returns></returns>
    public override SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobDTO)
    {
        var taskInfo = new SubmittedTaskInfo();
        taskInfo.ScheduledJobId = jobDTO.SchedulerJobId;
        taskInfo.Name = jobDTO.Name;
        taskInfo.State = jobDTO.TaskState;
        taskInfo.StartTime = jobDTO.StartTime;
        taskInfo.EndTime = jobDTO.EndTime;
        taskInfo.AllocatedTime = jobDTO.AllocatedTime.TotalSeconds;
        taskInfo.TaskAllocationNodes = taskInfo.TaskAllocationNodes.Select(s => new SubmittedTaskAllocationNodeInfo
            {
                AllocationNodeId = s.ToString(),
                SubmittedTaskInfoId = long.Parse(taskInfo.Name)
            })
            .ToList();
        taskInfo.AllParameters = jobDTO.SchedulerResponseParameters;
        return taskInfo;
    }

    /// <summary>
    ///     Read Parameters From Response
    /// </summary>
    /// <param name="cluster">Cluster</param>
    /// <param name="response">Scheduler response message</param>
    /// <returns></returns>
    public override IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object response)
    {
        List<SubmittedTaskInfo> taskInfos = new();
        var jobsAdapter = JsonSerializer.Deserialize<LinuxLocalInfo>(response.ToString());
        var allTasks = jobsAdapter.Jobs;
        taskInfos.AddRange(ConvertTasksToTaskInfoCollection(jobsAdapter, allTasks));

        return taskInfos;
    }

    /// <summary>
    ///     Convert JobSpecification ToJob
    /// </summary>
    /// <param name="jobSpecification">Internal Job Specification</param>
    /// <param name="schedulerAllocationCmd">Scheduler command</param>
    /// <returns></returns>
    public override object ConvertJobSpecificationToJob(JobSpecification jobSpecification,
        object schedulerAllocationCmd = null)
    {
        var localHpcJobInfo = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            jobSpecification.ConvertToLocalHPCInfo(LinuxLocalTaskState.Q.ToString(), LinuxLocalTaskState.Q.ToString()))
        );
        StringBuilder commands = new();
        StringBuilder taskCommandLine = new();
        foreach (var task in jobSpecification.Tasks)
        {
            var commandParameterDictionary = CreateTemplateParameterValuesDictionary(
                jobSpecification,
                task,
                task.CommandTemplate.TemplateParameters,
                task.CommandParameterValues
            );
            taskCommandLine.Append(ReplaceTemplateDirectivesInCommand(
                $"{task.CommandTemplate.ExecutableFile} {task.CommandTemplate.CommandParameters}",
                commandParameterDictionary));

            if (!string.IsNullOrEmpty(task.StandardOutputFile))
                taskCommandLine.Append($" 1>>{task.StandardOutputFile}");
            if (!string.IsNullOrEmpty(task.StandardErrorFile)) taskCommandLine.Append($" 2>>{task.StandardErrorFile}");
            commands.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(taskCommandLine.ToString())) + " ");
            taskCommandLine.Clear();
        }

        var localBasePath = $"{jobSpecification.Cluster.ClusterProjects
            .Find(cp => cp.ProjectId == jobSpecification.ProjectId)?.LocalBasepath}";

        var jobDir = Path.Join(localBasePath, HPCConnectionFrameworkConfiguration.ScriptsSettings.SubExecutionsPath,
            jobSpecification.Id.ToString()).Replace('\\', '/');
        //preparation script, prepares job info file to the job directory at local linux "cluster"
        return
            $"{_scripts.LinuxLocalCommandScriptPathSettings.ScriptsBasePath}/{_linuxLocalCommandScripts.PrepareJobDirCmdScriptName} {jobDir} {localHpcJobInfo} \"{commands}\";";
    }

    #endregion
}
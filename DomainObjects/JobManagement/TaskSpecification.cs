using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DomainObjects.JobManagement;

[Table("TaskSpecification")]
public class TaskSpecification : CommonTaskProperties
{
    [StringLength(40)] public string JobArrays { get; set; }

    [StringLength(40)] public string PlacementPolicy { get; set; }

    public bool IsExclusive { get; set; }

    public bool IsRerunnable { get; set; }

    [StringLength(30)] public string StandardInputFile { get; set; }

    [StringLength(30)] public string StandardOutputFile { get; set; }

    [StringLength(30)] public string StandardErrorFile { get; set; }

    [StringLength(200)] public string LocalDirectory { get; set; }

    [StringLength(50)] public string ClusterTaskSubdirectory { get; set; }

    public bool? CpuHyperThreading { get; set; }

    public virtual List<TaskSpecificationRequiredNode> RequiredNodes { get; set; } = new();

    [ForeignKey("ClusterNodeType")] public long ClusterNodeTypeId { get; set; }

    public virtual ClusterNodeType ClusterNodeType { get; set; }

    [ForeignKey("CommandTemplateId")] public long CommandTemplateId { get; set; }

    public virtual CommandTemplate CommandTemplate { get; set; }

    public virtual List<CommandTemplateParameterValue> CommandParameterValues { get; set; } = new();

    public virtual FileSpecification ProgressFile { get; set; }

    public virtual FileSpecification LogFile { get; set; }

    public virtual JobSpecification JobSpecification { get; set; }

    public virtual List<TaskParalizationSpecification> TaskParalizationSpecifications { get; set; } = new();

    public virtual List<TaskDependency> DependsOn { get; set; } = new();

    public virtual List<TaskDependency> Depended { get; set; } = new();

    #region Override methods

    public override string ToString()
    {
        var result = new StringBuilder("TaskSpecification: ");
        result.AppendLine("Id=" + Id);
        result.AppendLine("Name=" + Name);
        result.AppendLine("MinCores=" + MinCores);
        result.AppendLine("MaxCores=" + MaxCores);
        result.AppendLine("WalltimeLimit=" + WalltimeLimit);
        result.AppendLine("JobArrays=" + JobArrays);
        result.AppendLine("IsExclusive=" + IsExclusive);
        result.AppendLine("IsRerunnable=" + IsRerunnable);
        result.AppendLine("StandardInputFile=" + StandardInputFile);
        result.AppendLine("StandardOutputFile=" + StandardOutputFile);
        result.AppendLine("StandardErrorFile=" + StandardErrorFile);
        result.AppendLine("LocalDirectory=" + LocalDirectory);
        result.AppendLine("ClusterTaskSubdirectory=" + ClusterTaskSubdirectory);
        result.AppendLine("NodeType=" + ClusterNodeType);
        result.AppendLine("CommandTemplate=" + CommandTemplate);
        var i = 0;
        if (CommandParameterValues != null)
        {
            foreach (var parameterValue in CommandParameterValues)
                result.AppendLine("CommandParameterValue" + i++ + ":" + parameterValue);
            i = 0;
        }

        if (RequiredNodes != null)
        {
            foreach (var requiredNode in RequiredNodes)
                result.AppendLine("TaskSpecificationRequiredNode" + i++ + ": " + requiredNode);
            i = 0;
        }

        if (TaskParalizationSpecifications != null)
        {
            foreach (var parSpec in TaskParalizationSpecifications)
                result.AppendLine("TaskParalizationSpecification" + i++ + ": " + parSpec);
            i = 0;
        }

        if (EnvironmentVariables != null)
        {
            foreach (var variable in EnvironmentVariables)
                result.AppendLine("EnvironmentVariable" + i++ + ": " + variable);
            i = 0;
        }

        result.AppendLine("ProgressFile=" + ProgressFile);
        result.AppendLine("LogFile=" + LogFile);
        if (DependsOn != null)
            foreach (var depends in DependsOn)
                result.AppendLine("DependsOn" + i++ + ":" + depends.ParentTaskSpecificationId);
        return result.ToString();
    }

    #endregion

    #region Public methods

    public TaskSpecification()
    {
    }

    /// <summary>
    ///     Usage: Dividing task into smaller parts (Extra Long Tasks)
    ///     Warning: copy some references! -> DB 'static' values
    /// </summary>
    /// <param name="taskSpecification"></param>
    public TaskSpecification(TaskSpecification taskSpecification) : base(taskSpecification)
    {
        JobArrays = taskSpecification.JobArrays;
        PlacementPolicy = taskSpecification.PlacementPolicy;
        IsExclusive = taskSpecification.IsExclusive;
        IsRerunnable = taskSpecification.IsRerunnable;
        StandardInputFile = taskSpecification.StandardInputFile;
        StandardOutputFile = taskSpecification.StandardOutputFile;
        StandardErrorFile = taskSpecification.StandardErrorFile;
        LocalDirectory = taskSpecification.LocalDirectory;
        ClusterTaskSubdirectory = taskSpecification.ClusterTaskSubdirectory;
        CpuHyperThreading = taskSpecification.CpuHyperThreading;
        ProgressFile = taskSpecification.ProgressFile;
        LogFile = taskSpecification.LogFile;
        JobSpecification = taskSpecification.JobSpecification;
        ClusterNodeTypeId = taskSpecification.ClusterNodeTypeId;
        ClusterNodeType = taskSpecification.ClusterNodeType;
        CommandTemplateId = taskSpecification.CommandTemplateId;
        CommandTemplate = taskSpecification.CommandTemplate;

        //lists
        RequiredNodes = taskSpecification.RequiredNodes?
            .Select(x => new TaskSpecificationRequiredNode(x))
            .ToList();

        CommandParameterValues = taskSpecification.CommandParameterValues?
            .Select(x => new CommandTemplateParameterValue(x))
            .ToList();

        TaskParalizationSpecifications = taskSpecification.TaskParalizationSpecifications?
            .Select(x => new TaskParalizationSpecification(x))
            .ToList();

        DependsOn = taskSpecification.DependsOn?
            .Select(x => new TaskDependency(x))
            .ToList();

        Depended = taskSpecification.Depended?
            .Select(x => new TaskDependency(x))
            .ToList();
    }

    #endregion
}
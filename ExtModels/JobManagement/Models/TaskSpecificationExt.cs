using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.Utils;

namespace HEAppE.ExtModels.JobManagement.Models;

/// <summary>
/// Task specification model
/// </summary>
[DataContract(Name = "TaskSpecificationExt")]
[Description("Task specification model")]
public class TaskSpecificationExt
{
    /// <summary>
    /// Name
    /// </summary>
    [DataMember(Name = "Name")]
    [StringLength(50)]
    [Description("Name")]
    public string Name { get; set; }

    /// <summary>
    /// Minimum number of cores
    /// </summary>
    [DataMember(Name = "MinCores")]
    [Description("Minimum number of cores")]
    public int? MinCores { get; set; }

    /// <summary>
    /// Maximum number of cores
    /// </summary>
    [DataMember(Name = "MaxCores")]
    [Required]
    [Description("Maximum number of cores")]
    public int MaxCores { get; set; }

    /// <summary>
    /// Walltime limit
    /// </summary>
    [DataMember(Name = "WalltimeLimit")]
    [Description("Walltime limit")]
    public int? WalltimeLimit { get; set; }

    /// <summary>
    /// Placement policy
    /// </summary>
    [DataMember(Name = "PlacementPolicy")]
    [StringLength(40)]
    [Description("Placement policy")]
    public string PlacementPolicy { get; set; }

    /// <summary>
    /// Priority
    /// </summary>
    [DataMember(Name = "Priority")]
    [Description("Priority")]
    public TaskPriorityExt? Priority { get; set; }

    /// <summary>
    /// Job arrays
    /// </summary>
    [DataMember(Name = "JobArrays")]
    [Description("Job arrays")]
    public string JobArrays { get; set; }

    /// <summary>
    /// Is exclusive
    /// </summary>
    [DataMember(Name = "IsExclusive")]
    [Description("Is exclusive")]
    public bool? IsExclusive { get; set; } = false;

    /// <summary>
    /// Is rerunnable
    /// </summary>
    [DataMember(Name = "IsRerunnable")]
    [Description("Is rerunnable")]
    public bool? IsRerunnable { get; set; } = false;

    /// <summary>
    /// Standard input file
    /// </summary>
    [DataMember(Name = "StandardInputFile")]
    [StringLength(30)]
    [Description("Standard input file")]
    public string StandardInputFile { get; set; }

    /// <summary>
    /// Standard output file
    /// </summary>
    [DataMember(Name = "StandardOutputFile")]
    [StringLength(30)]
    [Description("Standard output file")]
    public string StandardOutputFile { get; set; }

    /// <summary>
    /// Standard error file
    /// </summary>
    [DataMember(Name = "StandardErrorFile")]
    [StringLength(30)]
    [Description("Standard error file")]
    public string StandardErrorFile { get; set; }

    /// <summary>
    /// Progress file
    /// </summary>
    [DataMember(Name = "ProgressFile")]
    [Description("Progress file")]
    public string ProgressFile { get; set; }

    /// <summary>
    /// Log file
    /// </summary>
    [DataMember(Name = "LogFile")]
    [Description("Log file")]
    public string LogFile { get; set; }

    /// <summary>
    /// Cluster task subdirectory
    /// </summary>
    [DataMember(Name = "ClusterTaskSubdirectory")]
    [StringLength(50)]
    [Description("Cluster task subdirectory")]
    public string ClusterTaskSubdirectory { get; set; }

    /// <summary>
    /// Cluster node type id
    /// </summary>
    [DataMember(Name = "ClusterNodeTypeId")]
    [Description("Cluster node type id")]
    public long? ClusterNodeTypeId { get; set; }

    /// <summary>
    /// Command template id
    /// </summary>
    [DataMember(Name = "CommandTemplateId")]
    [Description("Command template id")]
    public long? CommandTemplateId { get; set; }

    /// <summary>
    /// Cpu hyper threading
    /// </summary>
    [DataMember(Name = "CpuHyperThreading")]
    [Description("Cpu hyper threading")]
    public bool? CpuHyperThreading { get; set; }

    /// <summary>
    /// Required nodes
    /// </summary>
    [DataMember(Name = "RequiredNodes")]
    [Description("Required nodes")]
    public string[] RequiredNodes { get; set; }

    /// <summary>
    /// Array of task paralelization parameters
    /// </summary>
    [DataMember(Name = "TaskParallelizationParameters")]
    [Description("Array of task paralelization parameters")]
    public TaskParalizationParameterExt[] TaskParallelizationParameters { get; set; }

    /// <summary>
    /// Array of environment variables
    /// </summary>
    [DataMember(Name = "EnvironmentVariables")]
    [Description("Array of environment variables")]
    public EnvironmentVariableExt[] EnvironmentVariables { get; set; }

    /// <summary>
    /// Depends on
    /// </summary>
    [DataMember(Name = "DependsOn")]
    [Description("Depends on")]
    public TaskSpecificationExt[] DependsOn { get; set; }

    /// <summary>
    /// Array of command template parameter values
    /// </summary>
    [DataMember(Name = "TemplateParameterValues")]
    [Description("Array of command template parameter values")]
    public CommandTemplateParameterValueExt[] TemplateParameterValues { get; set; }

    public override string ToString()
    {
        return
            $"TaskSpecificationExt(name={Name}; minCores={MinCores}; maxCores={MaxCores}; walltimeLimit={WalltimeLimit}; requiredNodes={RequiredNodes}; priority={Priority}; jobArrays={JobArrays}; isExclusive={IsExclusive}; isRerunnable={IsRerunnable}; standardInputFile={StandardInputFile}; standardOutputFile={StandardOutputFile}; standardErrorFile={StandardErrorFile}; progressFile={ProgressFile}; logFile={LogFile}; clusterTaskSubdirectory={ClusterTaskSubdirectory}; clusterNodeTypeId={ClusterNodeTypeId}; commandTemplateId={CommandTemplateId}; taskParalizationParameters={TaskParallelizationParameters}; environmentVariables={EnvironmentVariables}; dependsOn={DependsOn}; templateParameterValues={TemplateParameterValues})";
    }

    public override bool Equals(object obj)
    {
        var x = this.CompareRecursive(obj);
        return x;
    }

    public override int GetHashCode()
    {
        return this.GetObjectHashCodeRecursive();
    }
}
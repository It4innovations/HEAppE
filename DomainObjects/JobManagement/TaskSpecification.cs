using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("TaskSpecification")]
    public class TaskSpecification : CommonTaskProperties
    {
        [StringLength(40)]
        public string JobArrays { get; set; }

        [StringLength(40)]
        public string PlacementPolicy { get; set; }

        public bool IsExclusive { get; set; }

        public bool IsRerunnable { get; set; }

        [StringLength(30)]
        public string StandardInputFile { get; set; }

        [StringLength(30)]
        public string StandardOutputFile { get; set; }

        [StringLength(30)]
        public string StandardErrorFile { get; set; }

        [StringLength(200)]
        public string LocalDirectory { get; set; }

        [StringLength(50)]
        public string ClusterTaskSubdirectory { get; set; }

        public bool? CpuHyperThreading { get; set; }

        public virtual List<TaskSpecificationRequiredNode> RequiredNodes { get; set; } = new List<TaskSpecificationRequiredNode>();

        [ForeignKey("ClusterNodeType")]
        public long ClusterNodeTypeId { get; set; }

        public virtual ClusterNodeType ClusterNodeType { get; set; }

        [ForeignKey("CommandTemplateId")]
        public long CommandTemplateId { get; set; }
        public virtual CommandTemplate CommandTemplate { get; set; }

        public virtual List<CommandTemplateParameterValue> CommandParameterValues { get; set; } = new List<CommandTemplateParameterValue>();

        public virtual FileSpecification ProgressFile { get; set; }

        public virtual FileSpecification LogFile { get; set; }

        public virtual JobSpecification JobSpecification { get; set; }

        public virtual List<TaskParalizationSpecification> TaskParalizationSpecifications { get; set; } = new List<TaskParalizationSpecification>();

        public virtual List<TaskDependency> DependsOn { get; set; } = new List<TaskDependency>();

        public virtual List<TaskDependency> Depended { get; set; } = new List<TaskDependency>();

        #region Public methods
        public TaskSpecification() : base() { }

        /// <summary>
        /// Usage: Dividing task into smaller parts (Extra Long Tasks)
        /// Warning: copy some references! -> DB 'static' values
        /// </summary>
        /// <param name="taskSpecification"></param>
        public TaskSpecification(TaskSpecification taskSpecification) : base(taskSpecification)
        {
            this.JobArrays = taskSpecification.JobArrays;
            this.PlacementPolicy = taskSpecification.PlacementPolicy;
            this.IsExclusive = taskSpecification.IsExclusive;
            this.IsRerunnable = taskSpecification.IsRerunnable;
            this.StandardInputFile = taskSpecification.StandardInputFile;
            this.StandardOutputFile = taskSpecification.StandardOutputFile;
            this.StandardErrorFile = taskSpecification.StandardErrorFile;
            this.LocalDirectory = taskSpecification.LocalDirectory;
            this.ClusterTaskSubdirectory = taskSpecification.ClusterTaskSubdirectory;
            this.CpuHyperThreading = taskSpecification.CpuHyperThreading;
            this.ProgressFile = taskSpecification.ProgressFile;
            this.LogFile = taskSpecification.LogFile;
            this.JobSpecification = taskSpecification.JobSpecification;
            this.ClusterNodeTypeId = taskSpecification.ClusterNodeTypeId;
            this.ClusterNodeType = taskSpecification.ClusterNodeType;
            this.CommandTemplateId = taskSpecification.CommandTemplateId;
            this.CommandTemplate = taskSpecification.CommandTemplate;

            //lists
            this.RequiredNodes = taskSpecification.RequiredNodes?
                .Select(x => new TaskSpecificationRequiredNode(x))
                .ToList();

            this.CommandParameterValues = taskSpecification.CommandParameterValues?
                .Select(x => new CommandTemplateParameterValue(x))
                .ToList();

            this.TaskParalizationSpecifications = taskSpecification.TaskParalizationSpecifications?
                .Select(x => new TaskParalizationSpecification(x))
                .ToList();

            this.DependsOn = taskSpecification.DependsOn?
                .Select(x => new TaskDependency(x))
                .ToList();

            this.Depended = taskSpecification.Depended?
                .Select(x => new TaskDependency(x))
                .ToList();

        }
        #endregion

        #region Override methods
        public override string ToString()
        {
            StringBuilder result = new StringBuilder("TaskSpecification: ");
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
            int i = 0;
            if (CommandParameterValues != null)
            {
                foreach (CommandTemplateParameterValue parameterValue in CommandParameterValues)
                {
                    result.AppendLine("CommandParameterValue" + (i++) + ":" + parameterValue);
                }
                i = 0;
            }
            if (RequiredNodes != null)
            {
                foreach (TaskSpecificationRequiredNode requiredNode in RequiredNodes)
                {
                    result.AppendLine("TaskSpecificationRequiredNode" + (i++) + ": " + requiredNode);
                }
                i = 0;
            }
            if (TaskParalizationSpecifications != null)
            {
                foreach (TaskParalizationSpecification parSpec in TaskParalizationSpecifications)
                {
                    result.AppendLine("TaskParalizationSpecification" + (i++) + ": " + parSpec);
                }
                i = 0;
            }
            if (EnvironmentVariables != null)
            {
                foreach (EnvironmentVariable variable in EnvironmentVariables)
                {
                    result.AppendLine("EnvironmentVariable" + (i++) + ": " + variable);
                }
                i = 0;
            }
            result.AppendLine("ProgressFile=" + ProgressFile);
            result.AppendLine("LogFile=" + LogFile);
            if (DependsOn != null)
            {
                foreach (TaskDependency depends in DependsOn)
                {
                    result.AppendLine("DependsOn" + (i++) + ":" + depends.ParentTaskSpecificationId);
                }
            }
            return result.ToString();
        }
        #endregion
    }
}
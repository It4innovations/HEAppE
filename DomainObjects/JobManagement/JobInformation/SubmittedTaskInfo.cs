using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement.JobInformation
{
    [Table("SubmittedTaskInfo")]
    public class SubmittedTaskInfo : IdentifiableDbEntity
    {
        public string ScheduledJobId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; }

        public TaskState State { get; set; }

        public TaskPriority Priority { get; set; }

        /// <summary>
        /// Runtime time at scheduler
        /// </summary>
        public double? AllocatedTime { get; set; }

        /// <summary>
        /// Runtime used cores at scheduler
        /// </summary>
        public int? AllocatedCores { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public bool? CpuHyperThreading { get; set; }

        public virtual List<SubmittedTaskAllocationNodeInfo> TaskAllocationNodes { get; set; } = new List<SubmittedTaskAllocationNodeInfo>();

        public virtual ClusterNodeType NodeType { get; set; }

        [StringLength(500)]
        public string ErrorMessage { get; set; }

        [Column(TypeName = "text")]
        public string AllParameters { get; set; }

        public virtual Project Project { get; set; }

        public virtual TaskSpecification Specification { get; set; }
    }
}
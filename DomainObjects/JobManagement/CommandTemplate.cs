using HEAppE.DomainObjects.ClusterInformation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("CommandTemplate")]
    public class CommandTemplate : IdentifiableDbEntity
    {
        [Required]
        [StringLength(80)]
        public string Name { get; set; }

        [Required]
        [StringLength(200)]
        public string Description { get; set; }

        [StringLength(100)]
        public string ExtendedAllocationCommand { get; set; }

        [Required]
        [StringLength(255)]
        public string ExecutableFile { get; set; }

        [StringLength(200)]
        public string CommandParameters { get; set; }

        [StringLength(500)]
        public string PreparationScript { get; set; }

        [Required]
        public bool IsGeneric { get; set; } = false;

        [Required]
        public bool IsEnabled { get; set; } = true;

        public virtual List<CommandTemplateParameter> TemplateParameters { get; set; } = new List<CommandTemplateParameter>();

        [ForeignKey("ClusterNodeType")]
        public long? ClusterNodeTypeId { get; set; }
        public virtual ClusterNodeType ClusterNodeType { get; set; }

        [ForeignKey("Project")]
        public long? ProjectId { get; set; }
        public virtual Project Project { get; set; }

        public override string ToString()
        {
            return String.Format("CommandTemplate: Id={0}, Name={1}, ExecutableFile={2}", Id, Name, ExecutableFile);
        }
    }
}
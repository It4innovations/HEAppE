using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("TaskDependency")]
    public class TaskDependency
    {
        public long TaskSpecificationId { get; set; }
        public virtual TaskSpecification TaskSpecification { get; set; }

        public long ParentTaskSpecificationId { get; set; }
        public virtual TaskSpecification ParentTaskSpecification { get; set; }


        public TaskDependency() { }
        public TaskDependency(TaskDependency taskDependency)
        {
            this.TaskSpecificationId = taskDependency.ParentTaskSpecificationId;
            this.ParentTaskSpecificationId = taskDependency.ParentTaskSpecificationId;
            this.TaskSpecification = new TaskSpecification(taskDependency.TaskSpecification);
            this.ParentTaskSpecification = new TaskSpecification(taskDependency.ParentTaskSpecification);
        }

    }
}

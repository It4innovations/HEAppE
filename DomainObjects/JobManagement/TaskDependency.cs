using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.JobManagement;

[Table("TaskDependency")]
public class TaskDependency
{
    public TaskDependency()
    {
    }

    public TaskDependency(TaskDependency taskDependency)
    {
        TaskSpecificationId = taskDependency.ParentTaskSpecificationId;
        ParentTaskSpecificationId = taskDependency.ParentTaskSpecificationId;
        TaskSpecification = new TaskSpecification(taskDependency.TaskSpecification);
        ParentTaskSpecification = new TaskSpecification(taskDependency.ParentTaskSpecification);
    }

    public long TaskSpecificationId { get; set; }
    public virtual TaskSpecification TaskSpecification { get; set; }

    public long ParentTaskSpecificationId { get; set; }
    public virtual TaskSpecification ParentTaskSpecification { get; set; }
}
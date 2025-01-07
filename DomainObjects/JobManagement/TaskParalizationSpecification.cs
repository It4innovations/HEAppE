using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HEAppE.DomainObjects.JobManagement;

[Table("TaskParalizationSpecification")]
public class TaskParalizationSpecification : IdentifiableDbEntity
{
    #region Override methods

    public override string ToString()
    {
        var result = new StringBuilder("TaskParalizationSpecification: ");
        result.AppendLine("Id=" + Id);
        result.AppendLine("MPIProcesses=" + MPIProcesses);
        result.AppendLine("OpenMPThreads=" + OpenMPThreads);
        result.AppendLine("MaxCores=" + MaxCores);
        return result.ToString();
    }

    #endregion

    #region Properties

    public int MaxCores { get; set; }

    public int? MPIProcesses { get; set; }

    public int? OpenMPThreads { get; set; }

    public TaskParalizationSpecification()
    {
    }

    public TaskParalizationSpecification(TaskParalizationSpecification taskParalizationSpecification)
        : base(taskParalizationSpecification)
    {
        MaxCores = taskParalizationSpecification.MaxCores;
        MPIProcesses = taskParalizationSpecification.MPIProcesses;
        OpenMPThreads = taskParalizationSpecification.OpenMPThreads;
    }

    #endregion
}
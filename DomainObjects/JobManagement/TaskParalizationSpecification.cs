using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace HEAppE.DomainObjects.JobManagement
{
    [Table("TaskParalizationSpecification")]
    public class TaskParalizationSpecification : IdentifiableDbEntity
    {
        #region Properties
        public int MaxCores { get; set; }

        public int? MPIProcesses { get; set; }

        public int? OpenMPThreads { get; set; }

        public TaskParalizationSpecification() : base() { }
        public TaskParalizationSpecification(TaskParalizationSpecification taskParalizationSpecification)
            : base(taskParalizationSpecification)
        {
            this.MaxCores = taskParalizationSpecification.MaxCores;
            this.MPIProcesses = taskParalizationSpecification.MPIProcesses;
            this.OpenMPThreads = taskParalizationSpecification.OpenMPThreads;
        }
        #endregion
        #region Override methods
        public override string ToString()
        {
            StringBuilder result = new StringBuilder("TaskParalizationSpecification: ");
            result.AppendLine("Id=" + Id);
            result.AppendLine("MPIProcesses=" + MPIProcesses);
            result.AppendLine("OpenMPThreads=" + OpenMPThreads);
            result.AppendLine("MaxCores=" + MaxCores);
            return result.ToString();
        }
        #endregion
    }
}

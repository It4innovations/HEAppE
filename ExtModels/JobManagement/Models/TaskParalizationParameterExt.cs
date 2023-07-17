using System.Runtime.Serialization;

namespace HEAppE.ExtModels.JobManagement.Models
{
    [DataContract(Name = "TaskParalizationParameterExt")]
    public class TaskParalizationParameterExt
    {
        [DataMember(Name = "MPIProcesses")]
        public int? MPIProcesses { get; set; }

        [DataMember(Name = "OpenMPThreads")]
        public int? OpenMPThreads { get; set; }

        [DataMember(Name = "MaxCores")]
        public int MaxCores { get; set; }

        public override string ToString()
        {
            return $"TaskParalizationParameterExt(MPIProcesses={MPIProcesses}; OpenMPThreads={OpenMPThreads}; MaxCores={MaxCores})";
        }
    }
}

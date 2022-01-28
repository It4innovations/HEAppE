using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.ConversionAdapter
{
    public interface ISchedulerTaskAdapter
    {
        object AllocationCmd { get; }

        TaskPriority Priority { set; }

        string Queue { set; }

        bool CpuHyperThreading { set; }

        string JobArrays { set; }

        string Name { set; }

        ICollection<TaskDependency> DependsOn { set; }

        bool IsExclusive { set; }

        bool IsRerunnable { set; }

        int Runtime { set; }

        string StdErrFilePath { set; }

        string StdInFilePath { set; }

        string StdOutFilePath { set; }

        string WorkDirectory { set; }

        void SetRequestedResourceNumber(ICollection<string> requestedNodeGroups, ICollection<string> requiredNodes, string placementPolicy, ICollection<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode);

        void SetEnvironmentVariablesToTask(ICollection<EnvironmentVariable> variables);

        void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile, string recursiveSymlinkCommand);
    }
}
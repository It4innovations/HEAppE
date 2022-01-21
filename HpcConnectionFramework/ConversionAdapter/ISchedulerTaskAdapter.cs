using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.ConversionAdapter
{
    public interface ISchedulerTaskAdapter
    {
        object AllocationCmd { get; }
        ICollection<string> AllocatedCoreIds { get; }
        string Name { get; set; }
        TaskState State { get; }
        DateTime? StartTime { get; }
        DateTime? EndTime { get; }
        string ErrorMessage { get; }

        string Id { get; }
        TaskPriority Priority { get; set; }
        string Queue { get; set; }
        string JobArrays { set; }
        ICollection<TaskDependency> DependsOn { set; }
        bool IsExclusive { set; }
        bool IsRerunnable { set; }
        int Runtime { get; set; }
        string StdErrFilePath { set; }
        string StdInFilePath { set; }
        string StdOutFilePath { set; }
        string WorkDirectory { set; }
        double AllocatedTime { get; }

        bool CpuHyperThreading { set; }

        Dictionary<string, string> AllParameters { get; }

        void SetRequestedResourceNumber(ICollection<string> requestedNodeGroups, ICollection<string> requiredNodes, string placementPolicy, ICollection<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode);

        void SetEnvironmentVariablesToTask(ICollection<EnvironmentVariable> variables);
        void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine, string stdOutFile, string stdErrFile, string recursiveSymlinkCommand);
    }
}
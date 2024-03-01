using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.HyperQueue.Generic.ConversionAdapter
{
    /// <summary>
    /// HyperQueue task adapter
    /// </summary>
    public class HyperQueueTaskAdapter : ISchedulerTaskAdapter
    {
        protected StringBuilder _taskBuilder = new();
        protected StringBuilder _hqAutoAllocParametersBuilder = new();

        private string _queueTimeLimit;

        public HyperQueueTaskAdapter(string taskSource)
        {
            _taskBuilder.Append($"{taskSource}");
        }

        public object AllocationCmd
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("ml HyperQueue");
                sb.Append(" && ");
                sb.Append("(nohup hq server start &)"); // Assuming this needs to run in the background
                sb.Append(" && ");
                sb.Append(" sleep 1 "); // Wait 1s when HQ Server starts
                sb.Append(" && ");
                sb.Append($"hq alloc add slurm {timeLimit} --{_hqAutoAllocParametersBuilder}");
                sb.Append(" && ");
                sb.Append(_taskBuilder);
                string commandLine = sb.ToString();
                return commandLine.Last().Equals(';') ? commandLine : $"{commandLine};";
            }
        }

        public TaskPriority Priority
        {
            set
            { 
                _taskBuilder.Append($" --priority={value}");
            }
        }

        public string Queue
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _hqAutoAllocParametersBuilder.Append($" -p {value}");
                }
            }
        }

        public string QualityOfService { get; set; }
        public string ClusterAllocationName { get; set; }
        public bool CpuHyperThreading { get; set; }
        public string JobArrays {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _taskBuilder.Append($" --array={value}");
                }
            }
        }
        public string Name {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _taskBuilder.Append($" --name={value}"); 
                }
            }
        }

        public string Project
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    _hqAutoAllocParametersBuilder.Append($" -A {value}");
                }
            }
        }

        public IEnumerable<TaskDependency> DependsOn { get; set; }
        public bool IsExclusive { get; set; }
        public bool IsRerunnable { get; set; }

        private string timeLimit {get; set;}
        public int Runtime
        {
            set
            {
                _taskBuilder.Append($" --time-limit={value}s");
                timeLimit = $"--time-limit={value}s";
            }
        }

    public string StdErrFilePath {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _taskBuilder.Append($" --stderr={value}");
            }
        }
    }
    public string StdInFilePath {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _taskBuilder.Append($" --stdin={value}");
            }
        }
    }
    public string StdOutFilePath {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _taskBuilder.Append($" --stdout={value}");
            }
        }
    }
    public string WorkDirectory {
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                _taskBuilder.Append($" --cwd={value}");
            }
        }
    }
    public string ExtendedAllocationCommand {
        get;
        set;
    }

    public void SetRequestedResourceNumber(IEnumerable<string> requestedNodeGroups, ICollection<string> requiredNodes,
        string placementPolicy,
        IEnumerable<TaskParalizationSpecification> paralizationSpecs, int minCores, int maxCores, int coresPerNode)
    {
        int nodeCount = maxCores / coresPerNode;
        nodeCount += maxCores % coresPerNode > 0 ? 1 : 0;
        
        if (placementPolicy.Contains("gpus"))
        {
            _hqAutoAllocParametersBuilder.Append($" --gpus={maxCores}");
            _taskBuilder.Append($" --resource {placementPolicy}={maxCores}");
        }
        else
        {
            _taskBuilder.Append($" --nodes={nodeCount}");
        }
    }

    public void SetEnvironmentVariablesToTask(IEnumerable<EnvironmentVariable> variables)
    {
        foreach (var variable in variables)
        {
            _taskBuilder.Append($" --env {variable.Name}={variable.Value}");
        }
    }

    public void SetPreparationAndCommand(string workDir, string preparationScript, string commandLine,
        string stdOutFile,
        string stdErrFile, string recursiveSymlinkCommand)
    {
        _taskBuilder.Append(
            string.IsNullOrEmpty(commandLine)
                ? string.Empty
                : commandLine.Last().Equals(';') ? $" {commandLine}" : $" {commandLine};");
    }
}

}
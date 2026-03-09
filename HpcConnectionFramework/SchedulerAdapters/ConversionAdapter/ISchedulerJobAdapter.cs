using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter;

/// <summary>
///     IScheduler job adapter
/// </summary>
public interface ISchedulerJobAdapter
{
    object AllocationCmd { get; }

    void SetTasks(IEnumerable<object> tasksAllocationcmd);

    void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure);

    void SetMemory(long memory);

    void SetMemoryPerCPU(long memoryPerCPU);

    void SetMemoryPerGPU(long memoryPerGPU);
}
using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal
{
    /// <summary>
    /// Mapper for Local Linux HPC formats
    /// </summary>
    internal static class Mapper
    {
        /// <summary>
        /// Map Local Linux Task state to HEAppE Task state
        /// </summary>
        /// <param name="taskState"></param>
        /// <returns></returns>
        internal static TaskState Map(this LinuxLocalTaskState taskState)
        {
            return taskState switch
            {
                LinuxLocalTaskState.H => TaskState.Configuring,
                LinuxLocalTaskState.Q => TaskState.Queued,
                LinuxLocalTaskState.O => TaskState.Failed,
                LinuxLocalTaskState.R => TaskState.Running,
                LinuxLocalTaskState.F => TaskState.Finished,
                LinuxLocalTaskState.S => TaskState.Canceled,
                _ => TaskState.Failed,
            };
        }
    }
}

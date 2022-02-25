using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal
{
    internal static class Mapper
    {
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

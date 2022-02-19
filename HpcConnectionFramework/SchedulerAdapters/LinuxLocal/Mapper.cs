using HEAppE.DomainObjects.JobManagement.JobInformation;
using HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.LinuxLocal
{
    internal static class Mapper
    {

        internal static TaskState Map(this LinuxLocalTaskState taskState)
        {
            switch (taskState)
            {
                case LinuxLocalTaskState.H:
                    return TaskState.Configuring;
                case LinuxLocalTaskState.Q:
                    return TaskState.Queued;
                case LinuxLocalTaskState.O:
                    return TaskState.Failed;
                case LinuxLocalTaskState.R:
                    return TaskState.Running;
                case LinuxLocalTaskState.F:
                    return TaskState.Finished;
                case LinuxLocalTaskState.S:
                    return TaskState.Canceled;
                default:
                    return TaskState.Failed;

            }
        }
    }
}

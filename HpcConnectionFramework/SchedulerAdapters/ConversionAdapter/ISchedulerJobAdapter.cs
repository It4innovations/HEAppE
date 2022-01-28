using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.ConversionAdapter
{
    public interface ISchedulerJobAdapter
    {
        object AllocationCmd { get; }

        void SetTasks(IEnumerable<object> list);

        void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure);
    }
}
using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.ConversionAdapter
{
    public interface ISchedulerJobAdapter
    {
        object AllocationCmd { get; }

        //string Name { get; set; }

        //string Project { get; set; }

        //JobState State { get; }

        //DateTime CreateTime { get; }

        //DateTime? SubmitTime { get; }

        //DateTime? StartTime { get; }

        //DateTime? EndTime { get; }

        void SetTasks(List<object> list);
        void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure);
    }
}
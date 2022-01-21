using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.ConversionAdapter
{
    public interface ISchedulerJobAdapter
    {
        string AccountingString { get; set; }

        object AllocationCmd { get; }

        string Id { get; }

        string Name { get; set; }

        string Project { get; set; }

        JobState State { get; }

        DateTime CreateTime { get; }

        DateTime? SubmitTime { get; }

        DateTime? StartTime { get; }

        DateTime? EndTime { get; }

        void SetTasks(List<object> list);

        List<object> GetTaskList();

        void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure);
    }
}
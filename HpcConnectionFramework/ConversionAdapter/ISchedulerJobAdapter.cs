using System;
using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.ConversionAdapter
{
    public interface ISchedulerJobAdapter
    {
        object Source { get; }
        string Id { get; }
        string Name { get; set; }
        string Project { get; set; }
        JobState State { get; }
        DateTime CreateTime { get; }
        DateTime? SubmitTime { get; }
        DateTime? StartTime { get; }
        DateTime? EndTime { get; }
        int Runtime { get; set; }
        string AccountingString { get; set; }

        List<object> GetTaskList();

        object CreateEmptyTaskObject();

        void SetNotifications(string mailAddress, bool? notifyOnStart, bool? notifyOnCompletion, bool? notifyOnFailure);

        void SetTasks(List<object> list);
    }
}
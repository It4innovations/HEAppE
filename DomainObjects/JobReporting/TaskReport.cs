using System.Runtime.Serialization;
using System;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobReporting
{
    public class TaskReport
    {
        public SubmittedTaskInfo SubmittedTaskInfo { get; set; }
        public double Usage { get; set; }
    }
}
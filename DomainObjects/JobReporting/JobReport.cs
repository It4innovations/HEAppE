using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.DomainObjects.JobReporting
{
    public class JobReport
    {
        public SubmittedJobInfo SubmittedJobInfo { get; set; }
        public List<TaskReport> Tasks { get; set; }
        public double? Usage => Math.Round(Tasks.Sum(x => x.Usage), 3);
    }
}
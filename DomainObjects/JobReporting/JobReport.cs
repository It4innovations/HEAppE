using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HEAppE.DomainObjects.JobReporting
{
    public class JobReport
    {
        public SubmittedJobInfo SubmittedJobInfo { get; set; }
        public List<TaskReport> Tasks { get; set; }
        public double? Usage => Tasks.Sum(x => x.Usage);
    }
}
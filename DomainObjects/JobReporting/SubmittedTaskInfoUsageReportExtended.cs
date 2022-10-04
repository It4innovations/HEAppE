using HEAppE.DomainObjects.JobManagement;
using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.DomainObjects.JobReporting
{
    public class SubmittedTaskInfoUsageReportExtended : SubmittedTaskInfoUsageReport
    {
        public long JobId { get; set; }

        public string JobName { get; set; }

        public Project Project { get; set; }
    }
}

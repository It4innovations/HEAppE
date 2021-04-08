using System;
using System.Collections.Generic;
using System.Text;

namespace HEAppE.DomainObjects.JobReporting
{
    public class SubmittedTaskInfoExtendedUsageReport : SubmittedTaskInfoUsageReport
    {
        public long JobId { get; set; }

        public string JobName { get; set; }
    }
}

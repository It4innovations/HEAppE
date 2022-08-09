using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DomainObjects.JobReporting
{
    public class JobStateAggregationReport
    {
        public JobState State { get; set; }

        public long Count { get; set; }
    }
}

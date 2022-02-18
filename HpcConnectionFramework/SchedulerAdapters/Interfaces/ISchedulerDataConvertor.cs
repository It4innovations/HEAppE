using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    public interface ISchedulerDataConvertor
    {
        object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd);

        object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object schedulerAllocationCmd);

        void FillingSchedulerJobResultObjectFromSchedulerAttribute(object schedulerResultObj, Dictionary<string, string> parsedParameters);

        abstract ClusterNodeUsage ReadQueueActualInformation(object responseMessage, ClusterNodeType nodeType);

        abstract IEnumerable<string> GetJobIds(string responseMessage);

        abstract SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo);

        abstract IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object responseMessage);
    }
}
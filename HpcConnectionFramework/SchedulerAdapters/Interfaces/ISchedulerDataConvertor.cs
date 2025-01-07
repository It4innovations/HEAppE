using System.Collections.Generic;
using HEAppE.DomainObjects.ClusterInformation;
using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces;

/// <summary>
///     IData convertor
/// </summary>
public interface ISchedulerDataConvertor
{
    object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd);

    object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification,
        object schedulerAllocationCmd);

    void FillingSchedulerJobResultObjectFromSchedulerAttribute(Cluster cluster, object schedulerResultObj,
        Dictionary<string, string> parsedParameters);

    ClusterNodeUsage ReadQueueActualInformation(ClusterNodeType nodeType, object responseMessage);

    IEnumerable<string> GetJobIds(string responseMessage);

    SubmittedTaskInfo ConvertTaskToTaskInfo(ISchedulerJobInfo jobInfo);

    IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(Cluster cluster, object responseMessage);
}
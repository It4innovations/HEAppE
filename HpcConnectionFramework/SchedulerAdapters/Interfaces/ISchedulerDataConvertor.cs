using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.HpcConnectionFramework.SchedulerAdapters.Interfaces
{
    public interface ISchedulerDataConvertor
    {

        object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object schedulerAllocationCmd);

        object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object schedulerAllocationCmd);

        abstract IEnumerable<string> GetJobIds(string responseMessage);

        public abstract SubmittedTaskInfo ConvertTaskToTaskInfo(object responseMessage);

        abstract IEnumerable<SubmittedTaskInfo> ReadParametersFromResponse(object responseMessage);
    }
}
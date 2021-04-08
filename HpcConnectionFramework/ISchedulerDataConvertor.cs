using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.HpcConnectionFramework {
	public interface ISchedulerDataConvertor {
		SubmittedJobInfo ConvertJobToJobInfo(object job);
		SubmittedTaskInfo ConvertTaskToTaskInfo(object task);
		object ConvertJobSpecificationToJob(JobSpecification jobSpecification, object job);
		object ConvertTaskSpecificationToTask(JobSpecification jobSpecification, TaskSpecification taskSpecification, object task);
	}
}
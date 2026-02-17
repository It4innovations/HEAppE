using System;
using System.Collections.Generic;
using System.Linq;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;

public interface ISubmittedJobInfoRepository : IRepository<SubmittedJobInfo>
{
    public SubmittedJobInfo GetBySubmittedTaskId(long taskId);
    IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId);
    IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId);
    IEnumerable<SubmittedJobInfo> GetAllUnfinished();
    IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount();

    public IEnumerable<SubmittedJobInfo> GetJobsForReport(DateTime startTime, DateTime endTime, long projectId,
        long nodeTypeId);

    public IQueryable<SubmittedJobInfo> GetJobsForUserQuery(long submitterId);

    public SubmittedJobInfo GetByIdWithTasks(long id);
}
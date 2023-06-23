using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation
{
    public interface ISubmittedJobInfoRepository : IRepository<SubmittedJobInfo>
    {
        IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId);
        IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId);
        IEnumerable<SubmittedJobInfo> GetAllUnfinished();
        IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount();
        IEnumerable<SubmittedJobInfo> GetAllWithSubmittedTaskAdaptorUserAndProject();
    }
}
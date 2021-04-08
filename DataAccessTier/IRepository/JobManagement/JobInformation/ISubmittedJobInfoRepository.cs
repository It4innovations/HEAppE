using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation
{
    public interface ISubmittedJobInfoRepository : IRepository<SubmittedJobInfo>
    {
        IEnumerable<SubmittedJobInfo> ListNotFinishedForSubmitterId(long submitterId);
        IEnumerable<SubmittedJobInfo> ListAllForSubmitterId(long submitterId);
        IEnumerable<SubmittedJobInfo> ListAllUnfinished();
    }
}
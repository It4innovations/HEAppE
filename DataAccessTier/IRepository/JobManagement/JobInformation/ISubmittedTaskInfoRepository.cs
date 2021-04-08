using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Collections.Generic;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation
{
    public interface ISubmittedTaskInfoRepository : IRepository<SubmittedTaskInfo>
    {
        IEnumerable<SubmittedTaskInfo> ListAllUnfinished();
    }
}
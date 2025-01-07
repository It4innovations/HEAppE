using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation;

public interface ISubmittedTaskInfoRepository : IRepository<SubmittedTaskInfo>
{
    IEnumerable<SubmittedTaskInfo> GetAllUnFinished();
    IEnumerable<SubmittedTaskInfo> GetAllFinished();
}
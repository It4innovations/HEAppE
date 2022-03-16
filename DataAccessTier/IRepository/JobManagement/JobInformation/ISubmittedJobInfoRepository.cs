﻿using System.Collections.Generic;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.DataAccessTier.IRepository.JobManagement.JobInformation
{
    public interface ISubmittedJobInfoRepository : IRepository<SubmittedJobInfo>
    {
        IEnumerable<SubmittedJobInfo> GetNotFinishedForSubmitterId(long submitterId);
        IEnumerable<SubmittedJobInfo> GetAllForSubmitterId(long submitterId);
        IEnumerable<SubmittedJobInfo> GetAllUnfinished();
        IEnumerable<SubmittedJobInfo> GetAllWaitingForServiceAccount();
    }
}
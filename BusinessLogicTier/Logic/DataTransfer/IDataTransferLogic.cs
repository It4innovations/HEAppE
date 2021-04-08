using System.Collections.Generic;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;

namespace HEAppE.BusinessLogicTier.Logic.DataTransfer
{
    public interface IDataTransferLogic
    {
        DataTransferMethod GetDataTransferMethod(string ipAddress, int port, SubmittedJobInfo jobInfo, AdaptorUser loggedUser);
        void EndDataTransfer(DataTransferMethod transferMethod, SubmittedJobInfo jobInfo, AdaptorUser loggedUser);
        string HttpGetToJobNode(string httpRequest, string[] headers, long submittedJobInfoId, string ipAddress);
        string HttpPostToJobNode(string httpRequest, string[] headers, string httpPayload, long submittedJobInfoId, string ipAddress);
        //int WriteDataToJobNode(byte[] data, long submittedJobInfoId, string ipAddress, string sessionCode, bool closeConnection);
        //byte[] ReadDataFromJobNode(long submittedJobInfoId, string ipAddress, string sessionCode);
        void CloseAllConnectionsForJob(SubmittedJobInfo jobInfo);
        List<long> GetJobIdsForOpenTunnels();
    }
}
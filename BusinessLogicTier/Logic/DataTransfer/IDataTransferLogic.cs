using System.Collections.Generic;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.JobManagement.JobInformation;
using System.Threading.Tasks;

namespace HEAppE.BusinessLogicTier.Logic.DataTransfer
{
    public interface IDataTransferLogic
    {
        DataTransferMethod GetDataTransferMethod(string nodeIPAddress, int nodePort,long submittedTaskInfoId, AdaptorUser loggedUser);

        void EndDataTransfer(DataTransferMethod transferMethod, AdaptorUser loggedUser);

        Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser);

        Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeader> headers, string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, AdaptorUser loggedUser);

        IEnumerable<long> GetTaskIdsWithOpenTunnels();

        void CloseAllTunnelsForTask(SubmittedTaskInfo taskInfo);
    }
}
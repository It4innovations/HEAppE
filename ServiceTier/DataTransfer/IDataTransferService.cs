using HEAppE.ExtModels.DataTransfer.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HEAppE.ServiceTier.DataTransfer
{
    public interface IDataTransferService 
    {
        DataTransferMethodExt GetDataTransferMethod(string nodeIPAddress, int nodePort, long submittedTaskInfoId, string sessionCode);
        void EndDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode);
        Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode);
        Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders, string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode);
	}
}
using System.Collections.Generic;
using System.Threading.Tasks;
using HEAppE.ExtModels.DataTransfer.Models;

namespace HEAppE.ServiceTier.DataTransfer;

public interface IDataTransferService
{
    DataTransferMethodExt RequestDataTransfer(string nodeIPAddress, int nodePort, long submittedTaskInfoId,
        string sessionCode);

    void CloseDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode);

    Task<string> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders,
        long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode);

    Task<string> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders, string httpPayload,
        long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode);
}
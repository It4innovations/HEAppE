using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.DomainObjects.DataTransfer;
using HEAppE.ExtModels.DataTransfer.Models;

namespace HEAppE.ServiceTier.DataTransfer;

public interface IDataTransferService
{
    Task<DataTransferMethodExt> RequestDataTransfer(string nodeIPAddress, int nodePort, long submittedTaskInfoId,
        string sessionCode);

    Task CloseDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode);

    Task<JobNodeHttpResponse> HttpGetToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders,
        long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode);

    Task<JobNodeHttpResponse> HttpPostToJobNodeAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders, string httpPayload,
        long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode);

    Task HttpPostToJobNodeStreamAsync(string httpRequest, IEnumerable<HTTPHeaderExt> httpHeaders,
        string httpPayload, long submittedTaskInfoId, string nodeIPAddress, int nodePort, string sessionCode,
        Stream responseStream, CancellationToken cancellationToken);
}
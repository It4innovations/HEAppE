using HEAppE.ExtModels.DataTransfer.Models;

namespace HEAppE.ServiceTier.DataTransfer
{
    public interface IDataTransferService 
    {
        DataTransferMethodExt GetDataTransferMethod(string ipAddress, int port, long submittedJobInfoId, string sessionCode);
        void EndDataTransfer(DataTransferMethodExt usedTransferMethod, string sessionCode);
        string HttpGetToJobNode(string httpRequest, string[] httpHeaders, long submittedJobInfoId, string ipAddress, string sessionCode);
        string HttpPostToJobNode(string httpRequest, string[] httpHeaders, string httpPayload, long submittedJobInfoId, string ipAddress, string sessionCode);
        //int? WriteDataToJobNode(byte[] data, long submittedJobInfoId, string ipAddress, bool closeConnection, string sessionCode);
        //byte[] ReadDataFromJobNode(long submittedJobInfoId, string ipAddress, string sessionCode);
	}
}
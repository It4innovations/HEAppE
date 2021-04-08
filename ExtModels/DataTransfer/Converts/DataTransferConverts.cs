using HEAppE.DomainObjects.DataTransfer;
using HEAppE.ExtModels.DataTransfer.Models;

namespace HEAppE.ExtModels.DataTransfer.Converts
{
    public static class DataTransferConverts
    {
        public static DataTransferMethodExt ConvertIntToExt(this DataTransferMethod obj)
        {
            DataTransferMethodExt convert = new DataTransferMethodExt()
            {
                SubmittedJobId = obj.SubmittedJobId,
                IpAddress = obj.IpAddress,
                Port = obj.Port
            };
            return convert;
        }

        public static DataTransferMethod ConvertExtToInt(this DataTransferMethodExt obj)
        {
            DataTransferMethod convert = new DataTransferMethod()
            {
                SubmittedJobId = obj.SubmittedJobId,
                IpAddress = obj.IpAddress,
                Port = obj.Port
            };
            return convert;
        }
    }
}

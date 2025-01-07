using HEAppE.DomainObjects.DataTransfer;
using HEAppE.ExtModels.DataTransfer.Models;

namespace HEAppE.ExtModels.DataTransfer.Converts;

public static class DataTransferConverts
{
    public static DataTransferMethodExt ConvertIntToExt(this DataTransferMethod obj)
    {
        var convert = new DataTransferMethodExt
        {
            SubmittedTaskId = obj.SubmittedTaskId,
            Port = obj.Port,
            NodeIPAddress = obj.NodeIPAddress,
            NodePort = obj.NodePort
        };
        return convert;
    }

    public static DataTransferMethod ConvertExtToInt(this DataTransferMethodExt obj)
    {
        var convert = new DataTransferMethod
        {
            SubmittedTaskId = obj.SubmittedTaskId,
            Port = obj.Port,
            NodeIPAddress = obj.NodeIPAddress,
            NodePort = obj.NodePort
        };
        return convert;
    }

    public static HTTPHeaderExt ConvertIntToExt(this HTTPHeader obj)
    {
        var convert = new HTTPHeaderExt
        {
            Name = obj.Name,
            Value = obj.Value
        };
        return convert;
    }

    public static HTTPHeader ConvertExtToInt(this HTTPHeaderExt obj)
    {
        var convert = new HTTPHeader
        {
            Name = obj.Name,
            Value = obj.Value
        };
        return convert;
    }
}
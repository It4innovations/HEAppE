namespace HEAppE.DomainObjects.DataTransfer;

public class DataTransferMethod
{
    public long SubmittedTaskId { get; set; }
    public int? Port { get; set; }
    public string NodeIPAddress { get; set; }
    public int? NodePort { get; set; }
}
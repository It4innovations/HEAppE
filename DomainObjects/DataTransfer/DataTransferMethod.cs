using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.DataTransfer
{
    [NotMapped]
    public class DataTransferMethod
    {
        public long SubmittedTaskId { get; set; }
        public int? Port { get; set; }
        public string NodeIPAddress { get; set; }
        public int? NodePort { get; set; }
    }
}
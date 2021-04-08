using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.DataTransfer {
    [NotMapped]
	public class DataTransferMethod {
        public long SubmittedJobId { get; set; }
        public string IpAddress { get; set; }
        public int Port { get; set; }
	}
}
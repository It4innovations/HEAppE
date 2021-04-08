using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.FileTransfer {
	[NotMapped]
	public class FullFileSpecification : FileSpecification {
		public string SourceDirectory { get; set; }
		public string DestinationDirectory { get; set; }
	}
}
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HEAppE.DomainObjects.FileTransfer {
	[Table("FileSpecification")]
	public class FileSpecification : IdentifiableDbEntity {
		[Required]
		[StringLength(50)]
		public string RelativePath { get; set; }

		public FileNameSpecification NameSpecification { get; set; }

		public FileSynchronizationType SynchronizationType { get; set; }

		public override string ToString() {
			return String.Format("FileSpecification: Id={0}, RelativePath={1}, NameSpecification={2}, SynchronizationType={3}", Id, RelativePath, NameSpecification,
				SynchronizationType);
		}
	}
}
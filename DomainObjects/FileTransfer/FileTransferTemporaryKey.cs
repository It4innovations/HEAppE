using HEAppE.DomainObjects.JobManagement.JobInformation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.DomainObjects.FileTransfer
{
	[Table("FileTransferTemporaryKey")]
	public class FileTransferTemporaryKey : IdentifiableDbEntity
	{
		[Required]
		[StringLength(1500)]
		public string PublicKey { get; set; }

		[Required]
		public DateTime AddedAt { get; set; }

		[Required]
		public bool IsDeleted { get; set; } = false;

		[Required]
		public virtual SubmittedJobInfo SubmittedJob { get; set; }
	}
}

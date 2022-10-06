using System.Collections.Generic;
using HEAppE.DomainObjects.FileTransfer;

namespace HEAppE.FileTransferFramework {
	public interface IFileSynchronizer {
		FullFileSpecification SyncFileInfo { get; set; }
		long Offset { get; set; }
		ICollection<JobFileContent> SynchronizeFiles(long clusterId, long projectId);
	}
}
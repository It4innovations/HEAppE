using System;

namespace HEAppE.DomainObjects.FileTransfer {
	[Flags]
	public enum FileTransferProtocol {
		NetworkShare = 1,
		SftpScp = 2,
		LocalSftpScp = 16
	}
}
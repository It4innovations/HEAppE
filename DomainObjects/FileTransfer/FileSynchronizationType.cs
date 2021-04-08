using System;

namespace HEAppE.DomainObjects.FileTransfer {
	[Flags]
	public enum FileSynchronizationType {
		Full = 1,
		IncrementalAppend = 2
	}
}
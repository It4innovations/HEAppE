using System;

namespace HEAppE.DomainObjects.JobManagement {
	[Flags]
	public enum PropertyChangeMethod {
		Append = 1,
		Rewrite = 2
	}
}
using System;

namespace HEAppE.DomainObjects.JobManagement.JobInformation {
	[Flags]
	public enum JobState {
		Configuring = 1,
		Submitted = 2,
		WaitingForUser = 4,
		Queued = 8,
		Running = 16,
		Finished = 32,
		Failed = 64,
		Canceled = 128,
	}
}
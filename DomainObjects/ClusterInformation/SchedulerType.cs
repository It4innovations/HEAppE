using System;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Flags]
	public enum SchedulerType {
		LinuxLocal = 1,
		PbsPro = 2,
        Slurm = 4
	}
}
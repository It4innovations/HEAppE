using System;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Flags]
	public enum SchedulerType {
		PbsPro = 2,
        Slurm = 4,
		LinuxLocal = 8
	}
}
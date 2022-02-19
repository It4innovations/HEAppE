using System;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Flags]
	public enum SchedulerType {
		PbsPro = 1,
		PbsProV19 = 2,
        Slurm = 4,
		LinuxLocal = 8
	}
}
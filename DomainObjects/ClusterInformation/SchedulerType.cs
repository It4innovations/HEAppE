using System;

namespace HEAppE.DomainObjects.ClusterInformation {
	[Flags]
	public enum SchedulerType {
		LinuxPbsProV10 = 1,
		LinuxPbsProV12 = 2,
		WindowsHpc = 4,
        LinuxSlurmV18 = 8
	}
}
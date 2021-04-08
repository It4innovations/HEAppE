using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.HpcConnectionFramework {
	internal struct SchedulerEndpoint {
		public SchedulerEndpoint(string masterNodeName, SchedulerType schedulerType) : this() {
			this.MasterNodeName = masterNodeName;

			this.SchedulerType = schedulerType;
		}

		public string MasterNodeName { get; private set; }

		public SchedulerType SchedulerType { get; private set; }

		public override bool Equals(object obj) {
			return (obj is SchedulerEndpoint) &&
			       (this.MasterNodeName.Equals(((SchedulerEndpoint) obj).MasterNodeName)) &&
			       (this.SchedulerType.Equals(((SchedulerEndpoint) obj).SchedulerType));
		}

		public override int GetHashCode() {
			unchecked {
				var hash = 17;
				hash = (23*hash) + this.MasterNodeName.GetHashCode();
				hash = (23*hash) + this.SchedulerType.GetHashCode();
				return hash;
			}
		}
	}
}
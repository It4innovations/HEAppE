using HaaSMiddleware.ConnectionPool;
using HaaSMiddleware.DomainObjects.ClusterInformation;
using Microsoft.Hpc.Scheduler;

namespace HaaSMiddleware.HpcConnectionFramework.WindowsHpc {
	public class WindowsHpcAPIConnector : IPoolableAdapter {
		public object CreateConnectionObject(string masterNodeName, ClusterAuthenticationCredentials credentials) {
			return new Scheduler();
		}

		public void Connect(object scheduler, string masterNodeName, ClusterAuthenticationCredentials credentials) {
			((IScheduler) scheduler).Connect(masterNodeName);
		}

		public void Disconnect(object scheduler) {
			((IScheduler) scheduler).Dispose();
		}
	}
}
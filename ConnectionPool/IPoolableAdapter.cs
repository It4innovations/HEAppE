using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool {
	public interface IPoolableAdapter {
		object CreateConnectionObject(string masterNodeName, string remoteTimeZone, ClusterAuthenticationCredentials clusterCredentials, int? port = null);

		void Connect(object connection, string masterNodeName, ClusterAuthenticationCredentials clusterCredentials);

		void Disconnect(object connection);
	}
}
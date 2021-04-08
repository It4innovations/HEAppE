using HEAppE.DomainObjects.ClusterInformation;

namespace HEAppE.ConnectionPool {
	public interface IConnectionPool {
		ConnectionInfo GetConnectionForUser(ClusterAuthenticationCredentials credentials);
		void ReturnConnection(ConnectionInfo schedulerConnection);
	}
}
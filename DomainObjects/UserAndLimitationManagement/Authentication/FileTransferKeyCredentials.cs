namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication {
	public class FileTransferKeyCredentials : AuthenticationCredentials {
		public string PrivateKey { get; set; }
		public string PublicKey { get; set; }
	}
}
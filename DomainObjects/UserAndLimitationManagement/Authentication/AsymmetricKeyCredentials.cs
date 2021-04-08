namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication {
	public class AsymmetricKeyCredentials : AuthenticationCredentials {
		public string PrivateKey { get; set; }
		public string PublicKey { get; set; }
	}
}
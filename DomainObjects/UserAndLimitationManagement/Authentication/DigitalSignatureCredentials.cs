namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication {
	public class DigitalSignatureCredentials : AuthenticationCredentials {
		public string SignedContent { get; set; }
		public byte[] DigitalSignature { get; set; }
	}
}
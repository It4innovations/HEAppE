namespace HEAppE.DomainObjects.UserAndLimitationManagement.Authentication
{
    public class OpenIdCredentials : AuthenticationCredentials
    {
        /// <summary>
        /// OpenId access token.
        /// </summary>
        public string OpenIdAccessToken { get; set; }
        
        /// <summary>
        /// OpenId refresh token.
        /// </summary>
        public string OpenIdRefreshToken { get; set; }
    }
}
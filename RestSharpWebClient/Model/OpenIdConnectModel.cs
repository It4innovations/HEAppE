namespace RestSharpWebClient.Model
{
    public class OpenIdConnectModel
    {

        public bool RequestWasSuccessful { get; set; }

        #region AuthenticationModel

        public string Username { get; set; }
        public string Password { get; set; }
        public string AuthenticationResult { get; set; } = string.Empty;
        #endregion

        #region TokenIntrospectionModel
        public string IntrospectionToken { get; set; }
        public string TokenIntrospectionResult { get; set; } = string.Empty;
        #endregion

        #region OpenStackAuthentication
        public string OpenStackKeycloakToken { get; set; }
        public string OpenStackResult { get; set; }
        #endregion
    }
}

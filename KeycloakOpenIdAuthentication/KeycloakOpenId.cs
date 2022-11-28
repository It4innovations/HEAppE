using HEAppE.KeycloakOpenIdAuthentication.Configuration;
using HEAppE.KeycloakOpenIdAuthentication.Exceptions;
using HEAppE.KeycloakOpenIdAuthentication.JsonTypes;
using HEAppE.RestUtils;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace HEAppE.KeycloakOpenIdAuthentication
{
    public sealed class KeycloakOpenId
    {
        #region Instances
        /// <summary>
        /// Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private readonly RestClient _basicRestClient;
        #endregion
        #region Properties
        public async Task<string> AuthenticateUserWithPassword(string username, string password) => (await AuthenticateUserWithPasswordImplAsync(username, password, KeycloakConfiguration.ClientId)).AccessToken;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public KeycloakOpenId()
        {
            if(string.IsNullOrEmpty(KeycloakConfiguration.BaseUrl))
            {
                throw new KeycloakOpenIdException($"Not specify URL address for authentication user by \"{KeycloakConfiguration.Protocol}\"");
            }

            if (KeycloakConfiguration.Protocol == "openid-connect")
            {
                var options = new RestClientOptions(KeycloakConfiguration.BaseUrl)
                {
                    Encoding = Encoding.UTF8,
                    CachePolicy = new CacheControlHeaderValue()
                    {
                        NoCache = true,
                        NoStore = true
                    },
                    MaxTimeout = KeycloakConfiguration.ConnectionTimeout
                };
                _basicRestClient = new RestClient(options);
            }
            else
            {
                throw new NotImplementedException("Other client protocol than 'open-id' connect is not implemented. Please switch the client to 'openid-connect'.");
            }

        }
        #endregion
        #region Methods
        /// <summary>
        /// Authenticate user with password for selected client.
        /// <note>This only supports Public clients, without client_secret.</note>
        /// </summary>
        /// <param name="username">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="clientId">Client name.</param>
        /// <returns></returns>
        /// <exception cref="KeycloakOpenIdException">When fails autentication</exception>
        private async Task<OpenIdUserAuthenticationResult> AuthenticateUserWithPasswordImplAsync(string username, string password, string clientId)
        {
            RestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.Post)
                                           .AddHeader("content-type", "application/x-www-form-urlencoded")
                                           .AddParameter("client_id", clientId, ParameterType.GetOrPost)
                                           .AddParameter("grant_type", "password", ParameterType.GetOrPost)
                                           .AddParameter("username", username, ParameterType.GetOrPost)
                                           .AddParameter("password", password, ParameterType.GetOrPost);

            var response = await _basicRestClient.ExecuteAsync(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For getting user information.
        /// </summary>
        /// <param name="offlineToken">OpenId offline-token</param>
        /// <returns>Introspected user information</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to get user information from token</exception>
        public async Task<KeycloakUserInfoResult> GetUserInfoAsync(string offlineToken)
        {
            _basicRestClient.Authenticator = new JwtAuthenticator(offlineToken);
            RestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/userinfo", Method.Post)
                                            .AddHeader("content-type", "application/x-www-form-urlencoded")
                                            .AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket", ParameterType.GetOrPost)
                                            .AddParameter("audience", KeycloakConfiguration.ClientId, ParameterType.GetOrPost)
                                            .AddParameter("scope", "openid", ParameterType.GetOrPost, false);
                                            

            var response = await _basicRestClient.ExecuteAsync(restRequest);
            return ParseHelper.ParseJsonOrThrow<KeycloakUserInfoResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For exchange access-token to offline-token.
        /// </summary>
        /// <param name="accessToken">OpenId access-token</param>
        /// <returns>Instrospected token information</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to exchange token</exception>
        public async Task<OpenIdUserAuthenticationResult> ExchangeTokenAsync(string accessToken)
        {
            var restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.Post)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                        .AddParameter("client_secret", KeycloakConfiguration.SecretId, ParameterType.GetOrPost)
                                        .AddParameter("client_id", KeycloakConfiguration.ClientId, ParameterType.GetOrPost)
                                        .AddParameter("scope", "offline_access openid", ParameterType.GetOrPost)
                                        .AddParameter("requested_token_type", "urn:ietf:params:oauth:token-type:refresh_token", ParameterType.GetOrPost)
                                        .AddParameter("subject_token_type", "urn:ietf:params:oauth:token-type:access_token", ParameterType.GetOrPost)
                                        .AddParameter("subject_token", accessToken, ParameterType.GetOrPost)
                                        .AddParameter("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange", ParameterType.GetOrPost);

            var response = await _basicRestClient.ExecuteAsync(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For access-token introspection.
        /// </summary>
        /// <param name="accessToken">OpenId access-token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to introspect token</exception>
        public async Task<KeycloakTokenIntrospectionResult> TokenIntrospectionAsync(string accessToken)
        {
            _basicRestClient.Authenticator = new HttpBasicAuthenticator(KeycloakConfiguration.ClientId, KeycloakConfiguration.SecretId);
            RestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token/introspect", Method.Post)
                                            .AddHeader("content-type", "application/x-www-form-urlencoded")
                                            .AddParameter("token", accessToken, ParameterType.GetOrPost);

            var response = await _basicRestClient.ExecuteAsync(restRequest);
            return ParseHelper.ParseJsonOrThrow<KeycloakTokenIntrospectionResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Refresh open-id access token with refresh token to obtain new access-token.
        /// </summary>
        /// <param name="refreshToken">OpenId refresh-token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to refresh the token</exception>
        public async Task<OpenIdUserAuthenticationResult> RefreshAccessTokenAsync(string refreshToken)
        {
            _basicRestClient.Authenticator = new HttpBasicAuthenticator(KeycloakConfiguration.ClientId, KeycloakConfiguration.SecretId);
            RestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.Post)
                                            .AddHeader("content-type", "application/x-www-form-urlencoded")
                                            .AddParameter("refresh_token", refreshToken, ParameterType.GetOrPost)
                                            .AddParameter("grant_type", "refresh_token", ParameterType.GetOrPost);

            var response = await _basicRestClient.ExecuteAsync(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Validation open-id introspected token
        /// </summary>
        /// <param name="introspectedToken">Introspected-token</param>
        /// <exception cref="KeycloakOpenIdException">When validation is not valid</exception>
        public static void ValidateUserToken(KeycloakTokenIntrospectionResult introspectedToken)
        {
            if (!(introspectedToken.Active && KeycloakConfiguration.AllowedClientIds.Contains(introspectedToken.ClientId) && introspectedToken.EmailVerified))
            {
                StringBuilder textBuilder = new();
                if (!introspectedToken.Active)
                {
                    textBuilder.AppendLine("Open-Id: User is not active!");
                }

                if (!introspectedToken.EmailVerified)
                {
                    textBuilder.AppendLine("Open-Id: User does not verified email!");
                }

                if (!KeycloakConfiguration.AllowedClientIds.Contains(introspectedToken.ClientId))
                {
                    textBuilder.AppendLine("Open-Id: User is not in allowed clientIds!");
                }

                throw new KeycloakOpenIdException(textBuilder.ToString());
            }
        }
        #endregion
    }
}
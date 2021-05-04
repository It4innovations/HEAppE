﻿using System;
using RestSharp;
using HEAppE.KeycloakOpenIdAuthentication.JsonTypes;
using System.Text.RegularExpressions;
using System.Net;
using HEAppE.RestUtils;
using HEAppE.KeycloakOpenIdAuthentication.Exceptions;
using RestSharp.Authenticators;
using System.Text;
using System.Net.Cache;
using HEAppE.KeycloakOpenIdAuthentication.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace HEAppE.KeycloakOpenIdAuthentication
{
    public sealed class KeycloakOpenId
    {
        #region Instances
        /// <summary>
        /// Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private readonly IRestClient _basicRestClient;

        /// <summary>
        /// User prefix in HEAppE Database
        /// </summary>
        private readonly string _userPrefix;
        #endregion
        #region Properties
        public string AuthenticateUserWithPassword(string username, string password) => AuthenticateUserWithPasswordImpl(username, password, KeycloakConfiguration.ClientId).AccessToken;
        #endregion
        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        public KeycloakOpenId()
        {
            if (KeycloakConfiguration.Protocol == "openid-connect")
            {
                _basicRestClient = new RestClient(KeycloakConfiguration.BaseUrl)
                {
                    Encoding = Encoding.UTF8,
                    CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
                };

                _userPrefix = KeycloakConfiguration.HEAppEUserPrefix;
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
        private OpenIdUserAuthenticationResult AuthenticateUserWithPasswordImpl(string username, string password, string clientId)
        {
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.POST)
                                       .AddHeader("content-type", "application/x-www-form-urlencoded")
                                        .AddXWwwFormUrlEncodedBody(("client_id", clientId),
                                                                   ("grant_type", "password"),
                                                                   ("username", username),
                                                                   ("password", password));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }


        /// <summary>
        /// For getting user info
        /// </summary>
        /// <param name="offlineToken">OpenId offline token</param>
        /// <returns>Introspected user information</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to get user information from token</exception>
        public KeycloakUserInfoResult GetUserInfo(string offlineToken)
        {
            _basicRestClient.Authenticator = new JwtAuthenticator(offlineToken);

            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/userinfo", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("audience", KeycloakConfiguration.ClientId),
                                                                    ("grant_type", "urn:ietf:params:oauth:grant-type:uma-ticket"));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<KeycloakUserInfoResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For changing access token to offline token
        /// </summary>
        /// <param name="accessToken">OpenId access token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to exchange token</exception>
        public OpenIdUserAuthenticationResult ExchangeToken(string accessToken)
        {
            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("client_secret", KeycloakConfiguration.SecretId),
                                                                    ("client_id", KeycloakConfiguration.ClientId),
                                                                    ("scope", "offline_access"),
                                                                    ("requested_token_type", "urn:ietf:params:oauth:token-type:refresh_token"),
                                                                    ("subject_token_type", "urn:ietf:params:oauth:token-type:access_token"),
                                                                    ("subject_token", accessToken),
                                                                    ("grant_type", "urn:ietf:params:oauth:grant-type:token-exchange"));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// For introspection token
        /// </summary>
        /// <param name="accessToken">OpenId access token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to introspect token</exception>
        public KeycloakTokenIntrospectionResult TokenIntrospection(string accessToken)
        {
            _basicRestClient.Authenticator = new HttpBasicAuthenticator(KeycloakConfiguration.ClientId, KeycloakConfiguration.SecretId);

            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token/introspect", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("token", accessToken));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<KeycloakTokenIntrospectionResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Refresh open-id access token with refresh token to obtain new access token
        /// </summary>
        /// <param name="refreshToken">OpenId refresh token</param>
        /// <returns>Instrospected token info</returns>
        /// <exception cref="KeycloakOpenIdException">When fails to refresh the token</exception>
        public OpenIdUserAuthenticationResult RefreshAccessToken(string refreshToken)
        {
            _basicRestClient.Authenticator = new HttpBasicAuthenticator(KeycloakConfiguration.ClientId, KeycloakConfiguration.SecretId);

            IRestRequest restRequest = new RestRequest($"realms/{KeycloakConfiguration.RealmName}/protocol/{KeycloakConfiguration.Protocol}/token", Method.POST)
                                        .AddHeader("content-type", "application/x-www-form-urlencoded")
                                         .AddXWwwFormUrlEncodedBody(("refresh_token", refreshToken),
                                                                    ("grant_type", "refresh_token"));

            var response = _basicRestClient.Execute(restRequest);
            return ParseHelper.ParseJsonOrThrow<OpenIdUserAuthenticationResult, KeycloakOpenIdException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        /// Create username for heappe account from openid token info
        /// <note>Email is not required when registering user inside keycloak</note>
        /// </summary>
        /// <param name="decodedAccessToken">Decoded JWT access token</param>
        /// <returns>Username for the keycloak openid user</returns>
        public string CreateOpenIdUsernameForHEAppE(KeycloakUserInfoResult userInfo)
        {
            return userInfo.EmailVerified && !string.IsNullOrWhiteSpace(userInfo.Email)
                 ? $"{_userPrefix}{userInfo.Email}"
                 : $"{_userPrefix}{Regex.Replace(userInfo.PreferredUsername, @"\s+", " ")}";
        }

        public void ValidateUserToken(KeycloakTokenIntrospectionResult tt)
        {

            IEnumerable<string> allowedClientIds = new List<string>() { "LEXIS_ORCHESTRATOR_YORC", "LEXIS_DDI_STAGING_API", "LEXIS_ORCHESTRATOR_BUSINESS_LOGIC", "admin-cli" };

            if (! (tt.Active && allowedClientIds.Contains(tt.ClientId) && tt.EmailVerified))
            {
                StringBuilder str = new ();
                if (!tt.Active)
                {
                    str.AppendLine("User is not active!");
                }

                if (!tt.EmailVerified)
                {
                    str.AppendLine("User does not verified email!");
                }

                if (!allowedClientIds.Contains(tt.ClientId))
                {
                    str.AppendLine("User does not in allowed clientIds!");
                }

                //TODO logging
                throw new KeycloakOpenIdException(str.ToString());
            }
        }
        #endregion
    }
}
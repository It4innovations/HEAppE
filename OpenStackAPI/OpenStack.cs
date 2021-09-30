using System;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Text;
using HEAppE.OpenStackAPI.Configuration;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.OpenStackAPI.Exceptions;
using HEAppE.OpenStackAPI.JsonTypes.Authentication;
using HEAppE.RestUtils;
using log4net;
using Newtonsoft.Json;
using RestSharp;

namespace HEAppE.OpenStackAPI
{
    /// <summary>
    /// OpenStack Wrapper
    /// </summary>
    public class OpenStack
    {
        #region Instances
        /// <summary>
        /// Logger
        /// </summary>
        protected readonly ILog _logger;

        /// <summary>
        /// Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private readonly IRestClient _basicRestClient;
        #endregion
        #region Constructor
        /// <summary>
        /// Create OpenStack API client.
        /// </summary>
        /// <param name="openStackAddress">OpenStack address..</param>
        public OpenStack(string openStackAddress)
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
            _basicRestClient = new RestClient($"{openStackAddress}:{OpenStackSettings.IdentityPort}/")
            {
                Encoding = Encoding.UTF8,
                CachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore)
            };
        }
        #endregion
        /// <summary>
        /// Authenticate the user with password for no specific scopes.
        /// </summary>
        /// <param name="serviceAcc">OpenStack service account.</param>
        /// <param name="project">OpenStack project.</param>
        /// <returns>Authentication response from the rest api with the authentication token.</returns>
        /// <exception cref="OpenStackAPIException">Is thrown when the request is malformed and the API returns non 201 code.</exception>
        public AuthenticationResponse Authenticate(OpenStackServiceAccDTO serviceAcc, OpenStackProjectDTO project)
        {
            var requestObject = AuthenticationRequest.CreateScopedAuthenticationPasswordRequest(serviceAcc, project);
            string requestBody = JsonConvert.SerializeObject(requestObject, IgnoreNullSerializer.Instance);

            IRestRequest request = new RestRequest($"v{OpenStackSettings.OpenStackVersion}/auth/tokens", Method.POST)
                                        .AddSerializedJsonBody(requestBody);

            IRestResponse response = _basicRestClient.Execute(request);
            AuthenticationResponse result = ParseHelper.ParseJsonOrThrow<AuthenticationResponse, OpenStackAPIException>(response, HttpStatusCode.Created);
            result.AuthToken = (string)response.Headers.Single(p => p.Name == "X-Subject-Token").Value;

            return result;
        }

        /// <summary>
        /// Create application credentials bound to service account with given unique name and expiration time.
        /// </summary>
        /// <param name="requestedUserName">Username of the requester.</param>
        /// <returns>Created application credentials.</returns>
        public ApplicationCredentialsDTO CreateApplicationCredentials(string requestedUserName, AuthenticationResponse authResponse)
        {
            string uniqueTokenName = requestedUserName + '_' + Guid.NewGuid();
            var sessionExpiresAt = DateTime.UtcNow.AddSeconds(OpenStackSettings.OpenStackSessionExpiration);

            var requestObject = ApplicationCredentialsRequest.CreateApplicationCredentialsRequest(uniqueTokenName, sessionExpiresAt);
            string requestBody = JsonConvert.SerializeObject(requestObject, IgnoreNullSerializer.Instance);

            IRestRequest restRequest = new RestRequest($"v{OpenStackSettings.OpenStackVersion}/users/{authResponse.Token.User.Id}/application_credentials", Method.POST)
                                        .AddSerializedJsonBody(requestBody)
                                        .AddXAuthTokenToHeader(authResponse.AuthToken);

            IRestResponse response = _basicRestClient.Execute(restRequest);
            ApplicationCredentialsResponse result = ParseHelper.ParseJsonOrThrow<ApplicationCredentialsResponse, OpenStackAPIException>(response, HttpStatusCode.Created);

            return new ApplicationCredentialsDTO
            {
                ApplicationCredentialsId = result.ApplicationCredentials.Id,
                ApplicationCredentialsSecret = result.ApplicationCredentials.Secret,
                ExpiresAt = sessionExpiresAt
            };
        }
    }
}
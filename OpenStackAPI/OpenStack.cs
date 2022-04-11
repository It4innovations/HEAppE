using HEAppE.OpenStackAPI.Configuration;
using HEAppE.OpenStackAPI.DTO;
using HEAppE.OpenStackAPI.Exceptions;
using HEAppE.OpenStackAPI.JsonTypes.Authentication;
using HEAppE.RestUtils;
using log4net;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
        private readonly RestClient _basicRestClient;
        #endregion
        #region Constructor
        /// <summary>
        /// Create OpenStack API client.
        /// </summary>
        /// <param name="openStackAddress">OpenStack address..</param>
        public OpenStack(string openStackAddress)
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            if (string.IsNullOrEmpty(openStackAddress))
            {
                throw new OpenStackAPIException("Not specify URL address for OpenStack");
            }

            var options = new RestClientOptions($"{openStackAddress}:{OpenStackSettings.IdentityPort}/")
            {
                Encoding = Encoding.UTF8,
                CachePolicy = new CacheControlHeaderValue()
                {
                    NoCache = true,
                    NoStore = true
                },
                Timeout = OpenStackSettings.ConnectionTimeout
            };
            _basicRestClient = new RestClient(options);
        }
        #endregion
        /// <summary>
        /// Authenticate the user with password for no specific scopes.
        /// </summary>
        /// <param name="serviceAcc">OpenStack service account.</param>
        /// <param name="project">OpenStack project.</param>
        /// <returns>Authentication response from the rest api with the authentication token.</returns>
        /// <exception cref="OpenStackAPIException">Is thrown when the request is malformed and the API returns non 201 code.</exception>
        public async Task<AuthenticationResponse> AuthenticateAsync(OpenStackServiceAccDTO serviceAcc, OpenStackProjectDTO project)
        {
            var requestObject = AuthenticationRequest.CreateScopedAuthenticationPasswordRequest(serviceAcc, project);
            string requestBody = JsonConvert.SerializeObject(requestObject, IgnoreNullSerializer.Instance);

            RestRequest request = new RestRequest($"v{OpenStackSettings.OpenStackVersion}/auth/tokens", Method.Post)
                                        .AddStringBody(requestBody, DataFormat.Json);

            RestResponse response = await _basicRestClient.ExecuteAsync(request);
            AuthenticationResponse result = ParseHelper.ParseJsonOrThrow<AuthenticationResponse, OpenStackAPIException>(response, HttpStatusCode.Created);
            result.AuthToken = (string)response.Headers.Single(p => p.Name == "X-Subject-Token").Value;

            return result;
        }

        /// <summary>
        /// Create application credentials bound to service account with given unique name and expiration time.
        /// </summary>
        /// <param name="requestedUserName">Username of the requester.</param>
        /// <returns>Created application credentials.</returns>
        public async Task<ApplicationCredentialsDTO> CreateApplicationCredentialsAsync(string requestedUserName, AuthenticationResponse authResponse)
        {
            string uniqueTokenName = requestedUserName + '_' + Guid.NewGuid();
            var sessionExpiresAt = DateTime.UtcNow.AddSeconds(OpenStackSettings.OpenStackSessionExpiration);

            var requestObject = ApplicationCredentialsRequest.CreateApplicationCredentialsRequest(uniqueTokenName, sessionExpiresAt);
            string requestBody = JsonConvert.SerializeObject(requestObject, IgnoreNullSerializer.Instance);

            RestRequest restRequest = new RestRequest($"v{OpenStackSettings.OpenStackVersion}/users/{authResponse.Token.User.Id}/application_credentials", Method.Post)
                                            .AddStringBody(requestBody, DataFormat.Json)
                                            .AddXAuthTokenToHeader(authResponse.AuthToken);

            RestResponse response = await _basicRestClient.ExecuteAsync(restRequest);
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
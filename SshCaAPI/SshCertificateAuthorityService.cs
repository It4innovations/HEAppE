using HEAppE.Exceptions.External;
using HEAppE.RestUtils;
using log4net;
using Newtonsoft.Json;
using RestSharp;
using SshCaAPI.Configuration;
using SshCaAPI.DTO.JsonTypes;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace SshCaAPI
{
    public class SshCertificateAuthorityService : ISshCertificateAuthorityService
    {
        /// <summary>
        ///     Logger
        /// </summary>
        protected readonly ILog _logger;

        /// <summary>
        ///     Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private readonly RestClient _basicRestClient;

        public SshCertificateAuthorityService()
        {
            _logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            var options = new RestClientOptions($"{SshCaSettings.BaseUri}/{SshCaSettings.CAName}/")
            {
                Encoding = Encoding.UTF8,
                CachePolicy = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                },
                Timeout = TimeSpan.FromMilliseconds(SshCaSettings.ConnectionTimeoutInSeconds * 1000)
            };
            _basicRestClient = new RestClient(options);
        }

        /// <summary>
        ///     Retrieve CA metadata and AAI configuration async
        /// </summary>
        /// <returns>Config response from the rest api.</returns>
        /// <exception cref="SshCAServiceTypeException">Is thrown when the request is malformed and the API returns non 201 code.</exception>
        public async Task<ConfigResponse> GetConfigAsync()
        {
            var request = new RestRequest($"config", Method.Get);
            var response = await _basicRestClient.ExecuteAsync(request);

            return ParseHelper.ParseJsonOrThrow<ConfigResponse, SshCAServiceTypeException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        ///     Sign certificate async
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="ott"></param>
        /// <returns>SSH certificate in OpenSSH certificate format.</returns>
        /// <exception cref="SshCAServiceTypeException">Is thrown when the request is malformed and the API returns non 201 code.</exception>
        public async Task<string> SignAsync(string publicKey, string ott)
        {
            var requestBody = JsonConvert.SerializeObject(new SignRequest { PublicKey = publicKey, Ott = ott },
                IgnoreNullSerializer.Instance);

            var request = new RestRequest($"sign", Method.Post)
                .AddStringBody(requestBody, DataFormat.Json);

            var response = await _basicRestClient.ExecuteAsync(request);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new SshCAServiceTypeException($"Unexpected status {response.StatusCode}");

            var result = response.Content;
            return result ?? throw new SshCAServiceTypeException("Response content is null");
        }
    }
}

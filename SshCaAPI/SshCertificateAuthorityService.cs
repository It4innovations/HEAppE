#pragma warning disable CS8602, CS8629, CS8604, CS8618
﻿using HEAppE.Exceptions.External;
using HEAppE.RestUtils;
using Microsoft.Extensions.Logging;
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
        ///     Get RestClient for the base keycloak url.
        /// </summary>
        /// <returns>Configured rest client.</returns>
        private readonly RestClient _basicRestClient;

        public SshCertificateAuthorityService(string baseUri, string caName, double connectionTimeoutInSeconds)
        {
            //caName can be empty, but baseUri cannot be empty. If baseUri is empty, the client will not be initialized and all API calls will fail, which is expected.
            string url = string.Empty;
            if (string.IsNullOrEmpty(caName))
            {
                url = baseUri;
            }
            else
            {
                url = $"{baseUri.TrimEnd('/')}/{caName}";
            }
            
            if (string.IsNullOrEmpty(baseUri) && string.IsNullOrEmpty(caName))
            {
                return;
            }
            var options = new RestClientOptions(url)
            {
                Encoding = Encoding.UTF8,
                CachePolicy = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true
                },
                Timeout = TimeSpan.FromMilliseconds(connectionTimeoutInSeconds * 1000)
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

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                throw new SshCAServiceTypeException("SshCertificateAuthorityService-GetConfig: Request timed out.") { Details = "Connection to SSH CA service timed out." };
            }

            return ParseHelper.ParseJsonOrThrow<ConfigResponse, SshCAServiceTypeException>(response, HttpStatusCode.OK);
        }

        /// <summary>
        ///     Sign certificate async
        /// </summary>
        /// <param name="publicKey"></param>
        /// <param name="ott"></param>
        /// <param name="resource"></param>
        /// <returns>SSH certificate in OpenSSH certificate format.</returns>
        /// <exception cref="SshCAServiceTypeException">Is thrown when the request is malformed and the API returns non 201 code.</exception>
        public async Task<SignResponse?> SignAsync(string publicKey, string ott, string resource, ILogger? logger)
        {
            logger?.LogInformation("[SignService] Method: SignAsync");

            var requestBody = JsonConvert.SerializeObject(new SignRequest { PublicKey = publicKey, Ott = ott, Resource = resource },
                IgnoreNullSerializer.Instance);

            var request = new RestRequest("signJSON", Method.Post)
                .AddStringBody(requestBody, DataFormat.Json);

            logger?.LogDebug($"[SignService Request] POST {_basicRestClient.BuildUri(request)} | Body: {requestBody}");

            var response = await _basicRestClient.ExecuteAsync(request);

            if (response.ResponseStatus == ResponseStatus.TimedOut)
            {
                logger?.LogError($"[SignService Timeout] Request to {_basicRestClient.BuildUri(request)} timed out.");
                throw new SshCAServiceTypeException("SshCertificateAuthorityService-Sign: Request timed out.") { Details = "Connection to SSH CA service timed out." };
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger?.LogError($"[SignService Error] Unexpected status={response.StatusCode}, Content={response.Content}");
                throw new SshCAServiceTypeException($"SshCertificateAuthorityService-Sign: Unexpected status={response.StatusCode}.") { Details = $"HTTP status={response.StatusCode}. Content: {response.Content}" };
            }

            logger?.LogDebug($"[SignService Response] Success ({response.StatusCode}). Content: {response.Content}");

            var json = JsonConvert.DeserializeObject<SignResponse>(response.Content ?? "{}");

            if (json?.SshCert != null && json.SshCert.EndsWith("\n"))
            {
                json.SshCert = json.SshCert.TrimEnd('\n');
            }

            return json;
        }

        /// <summary>
        ///    Get POSIX username for the provided token async
        /// </summary>
        /// <param name="token"></param>
        /// <param name="logger"></param>
        /// <returns>POSIX username or null if not found.</returns>
        public async Task<string?> GetPosixUsernameAsync(string token, ILogger? logger)
        {
            // For now, there is no direct endpoint to just get the username.
            // However, we can use the 'config' or 'sign' (if we had a key) to get it.
            // Given the requirement to "prepare it", I will leave this as a placeholder or 
            // try to see if any existing endpoint provides it.
            // Based on the current API, it's usually returned in SignResponse.
            return null; 
        }
    }
    
    public class SignResponse
    {
        [JsonProperty("ssh_cert")]
        public string SshCert { get; set; }

        [JsonProperty("resource")]
        public string Resource { get; set; }

        [JsonProperty("posix_username")]
        public string PosixUsername { get; set; }
    }

}

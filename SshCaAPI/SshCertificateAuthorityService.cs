#pragma warning disable CS8602, CS8629, CS8604, CS8618
﻿using System.IdentityModel.Tokens.Jwt;
using HEAppE.Exceptions.External;
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
        private readonly string? _caName;

        public SshCertificateAuthorityService(string baseUri, string caName, double connectionTimeoutInSeconds)
            : this(null, baseUri, caName, connectionTimeoutInSeconds)
        {
        }

        public SshCertificateAuthorityService(System.Net.Http.IHttpClientFactory? httpClientFactory, string baseUri, string caName, double connectionTimeoutInSeconds)
        {
            _caName = caName;
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

            if (httpClientFactory != null)
            {
                var httpClient = httpClientFactory.CreateClient("SshCaClient");
                _basicRestClient = new RestClient(httpClient, options);
            }
            else
            {
                _basicRestClient = new RestClient(options);
            }
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
                throw new SshCAServiceTypeException("GetConfigTimeout") { Details = "Connection to SSH CA service timed out." };
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
                throw new SshCAServiceTypeException("SignTimeout") { Details = "Connection to SSH CA service timed out." };
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger?.LogError($"[SignService Error] Unexpected status={response.StatusCode}, Content={response.Content}");
                throw new SshCAServiceTypeException("SignUnexpectedStatus", response.StatusCode) { Details = $"HTTP status={response.StatusCode}. Content: {response.Content}" };
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
        /// <param name="publicKey"></param>
        /// <param name="resource"></param>
        /// <returns>POSIX username or null if not found.</returns>
        public async Task<string?> GetPosixUsernameAsync(string token, ILogger? logger, string? publicKey = null, string? resource = null)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            // 1. Try to get posix_username from SSH CA signJSON endpoint if a public key is provided
            if (!string.IsNullOrWhiteSpace(publicKey))
            {
                try
                {
                    var resToUse = !string.IsNullOrWhiteSpace(resource)
                        ? resource
                        : (!string.IsNullOrWhiteSpace(_caName) ? _caName : "localhost");
                    var signResponse = await SignAsync(publicKey, token, resToUse, logger);

                    if (!string.IsNullOrEmpty(signResponse?.PosixUsername))
                    {
                        logger?.LogInformation($"[SshCertificateAuthorityService] Resolved POSIX username '{signResponse.PosixUsername}' from signJSON endpoint.");
                        return signResponse.PosixUsername;
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "[SshCertificateAuthorityService] Failed to resolve POSIX username via signJSON endpoint.");
                }
            }

            // 2. Fallback: extract username claims directly from JWT token
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                if (tokenHandler.CanReadToken(token))
                {
                    var jwt = tokenHandler.ReadJwtToken(token);
                    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "posix_username")
                             ?? jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")
                             ?? jwt.Claims.FirstOrDefault(c => c.Type == "username")
                             ?? jwt.Claims.FirstOrDefault(c => c.Type == "name");

                    if (!string.IsNullOrEmpty(claim?.Value))
                    {
                        logger?.LogInformation($"[SshCertificateAuthorityService] Resolved POSIX username '{claim.Value}' from JWT fallback claim '{claim.Type}'.");
                        return claim.Value;
                    }
                }
            }
            catch
            {
                // Ignore fallback exceptions
            }

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

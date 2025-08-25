using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.ExternalAuthentication.Configuration;
using IdentityModel.Client;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier;
public static class HttpContextKeys
{
    public static AdaptorUser AdaptorUser;
    public static string SshCaToken;

    public static async Task<AdaptorUser> Authorize(string token, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        using (var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork())
        {
            var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, sshCertificateAuthorityService);

            var user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
            {
                OpenIdLexisAccessToken = token
            });

            AdaptorUser = user;
            return user;
        }
    }

    public static async Task<string> ExchangeSshCaToken(string accessToken, string tokenExchangeAddress, HttpClient httpClient)
    {
        var clientId = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientId;
        var clientSecret = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientSecret;

        // Prepare basic auth header with clientId and clientSecret
        var authHeader = System.Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.GrantType,
            ["subject_token"] = accessToken,
            ["subject_token_type"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.SubjectTokenType,
            ["audience"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.Audience
        };

        var request = new HttpRequestMessage(HttpMethod.Post, tokenExchangeAddress)
        {
            Content = new FormUrlEncodedContent(form)
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        SshCaToken = tokenResponse.AccessToken;
        return tokenResponse.AccessToken;
    }

    // Helper class to deserialize the token response
    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("issued_token_type")]
        public string IssuedTokenType { get; set; }
        [JsonPropertyName("scope")]
        public string Scope { get; set; }
    }
}

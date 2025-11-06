using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HEAppE.BusinessLogicTier.Factory;
using HEAppE.DataAccessTier.Factory.UnitOfWork;
using HEAppE.DomainObjects.UserAndLimitationManagement;
using HEAppE.DomainObjects.UserAndLimitationManagement.Authentication;
using HEAppE.ExternalAuthentication.Configuration;
using IdentityModel.Client;
using log4net;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier;


public interface IRequestContext
{
    public long AdaptorUserId { get; set; } 
    public string SshCaToken { get; set; } 
    public string FIPToken { get; set; }
}

public class RequestContext : IRequestContext
{
    public long AdaptorUserId { get; set; } 
    public string SshCaToken { get; set; } 
    public string FIPToken { get; set; }
}

public interface IHttpContextKeys
{
    Task<AdaptorUser> Authorize(string token, ISshCertificateAuthorityService sshCertificateAuthorityService);
    Task<string> ExchangeSshCaToken(string accessToken, string tokenExchangeAddress, HttpClient httpClient);
    
    IRequestContext Context { get;  }
}


public class HttpContextKeys : IHttpContextKeys
{
    private readonly IRequestContext _context;
    public IRequestContext Context => _context;
    private readonly ILog _log;

    public HttpContextKeys(IRequestContext context)
    {
        _context = context;
        _log = LogManager.GetLogger(typeof(HttpContextKeys));
    }

    public async Task<AdaptorUser> Authorize(string token, ISshCertificateAuthorityService sshCertificateAuthorityService)
    {
        _log.Info("Authorizing with UserOrg");
        _context.FIPToken = token;

        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, sshCertificateAuthorityService, this);

        try
        {
            var user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
            {
                OpenIdLexisAccessToken = token
            });

            _context.AdaptorUserId = user.Id;
            return user;
        }
        catch (Exception ex)
        {
            _log.Error("Error during authorization", ex);
            throw;
        }
    }

    public async Task<string> ExchangeSshCaToken(string accessToken, string tokenExchangeAddress, HttpClient httpClient)
    {
        _log.Info($"Exchanging token for SSH CA token from {tokenExchangeAddress}");
        var clientId = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientId;
        var clientSecret = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientSecret;

        var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
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

        _context.SshCaToken = tokenResponse.AccessToken;
        return tokenResponse.AccessToken;
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; }
        [JsonPropertyName("token_type")] public string TokenType { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("issued_token_type")] public string IssuedTokenType { get; set; }
        [JsonPropertyName("scope")] public string Scope { get; set; }
    }
}


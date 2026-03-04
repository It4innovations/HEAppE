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
using HEAppE.DomainObjects.UserAndLimitationManagement.Enums;
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.UserOrg;
using IdentityModel.Client;
using log4net;
using Microsoft.Extensions.Options;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;


public interface IRequestContext
{
    public long AdaptorUserId { get; set; } 
    public string UserInfo { get; set; }
    public string SshCaToken { get; set; } 
    public string FIPToken { get; set; }
    public string LEXISToken { get; set; }
}

public class RequestContext : IRequestContext
{
    public long AdaptorUserId { get; set; } 
    public string UserInfo { get; set; }
    public string SshCaToken { get; set; } 
    public string FIPToken { get; set; }
    public string LEXISToken { get; set; }
}

public interface IHttpContextKeys
{
    Task<AdaptorUser> Authorize(ISshCertificateAuthorityService sshCertificateAuthorityService, IUserOrgService userOrgService);
    Task<string> ExchangeSshCaToken(string tokenExchangeAddress, HttpClient httpClient);
    
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

    public async Task<AdaptorUser> Authorize(ISshCertificateAuthorityService sshCertificateAuthorityService, IUserOrgService userOrgService)
    {
        _log.Info("[Authorize] Starting UserOrg authorization flow.");

        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork();
        var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, userOrgService, sshCertificateAuthorityService, this);
        AdaptorUser user = null;
        try
        {
            if (LexisAuthenticationConfiguration.UseBearerAuth)
            {
                _log.Info("[Authorize] Using Bearer authentication for Lexis.");
                user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
                {
                    OpenIdLexisAccessToken = Context.LEXISToken
                });
            }
            else if (JwtTokenIntrospectionConfiguration.IsEnabled)
            {
                bool useLexisToken = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled;
                _log.Info($"[Authorize] Using JWT introspection. LexisTokenFlowEnabled: {useLexisToken}");
                
                user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
                {
                    OpenIdLexisAccessToken = useLexisToken ? Context.LEXISToken : Context.FIPToken
                });
            }
            
            if(user != null)
            {
                _log.Info($"[Authorize] Success. User: {user.Username}:{user.Email} (ID: {user.Id})");
                _context.AdaptorUserId = user.Id;
                _context.UserInfo = $"{user.Username}:{user.Email}";
            }
            else
            {
                _log.Warn("[Authorize] Authorization returned null user.");
            }
            
            return user;
        }
        catch (Exception ex)
        {
            _log.Error("[Authorize] Exception occurred during authorization.", ex);
            throw;
        }
    }

    public async Task<string> ExchangeSshCaToken(string tokenExchangeAddress, HttpClient httpClient)
    {
        _log.Info($"[SshCaExchange Request] URL: {tokenExchangeAddress}");
        
        var clientId = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientId;
        var clientSecret = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientSecret;

        var authHeaderValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeaderValue);

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.GrantType,
            ["subject_token"] = Context.FIPToken,
            ["subject_token_type"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.SubjectTokenType,
            ["audience"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.Audience
        };

        LogRequestDetails(tokenExchangeAddress, form);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, tokenExchangeAddress)
            {
                Content = new FormUrlEncodedContent(form)
            };

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log.Error($"[SshCaExchange Response] Error: {response.StatusCode}, Content: {content}");
                response.EnsureSuccessStatusCode();
            }

            _log.Debug($"[SshCaExchange Response] Success: {response.StatusCode}");

            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _context.SshCaToken = tokenResponse.AccessToken;
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _log.Error("[SshCaExchange] Failed to exchange SSH CA token.", ex);
            throw;
        }
    }

    private void LogRequestDetails(string url, Dictionary<string, string> form)
    {
        _log.Debug($"[SshCaExchange Details] URL: {url}, GrantType: {form["grant_type"]}, Audience: {form["audience"]}");
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
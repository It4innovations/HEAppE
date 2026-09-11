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
using HEAppE.Exceptions.AbstractTypes;
using HEAppE.Exceptions.External; // Důležité pro vyhazování známých výjimek
using HEAppE.ExternalAuthentication;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.Services.Expirio;
using HEAppE.Services.UserOrg;
using Microsoft.Extensions.Logging;
using SshCaAPI;

namespace HEAppE.BusinessLogicTier.AuthMiddleware;

public interface IRequestContext
{
    public long AdaptorUserId { get; set; } 
    public string UserName { get; set; }
    public string Email { get; set; }
    public string UserInfo { get; set; }
    public string SshCaToken { get; set; } 
    public string IdpToken { get; set; }
    public string LEXISToken { get; set; }

    /// <summary>
    /// UserOrg token – backed by ExchangedTokens["userorg"].
    /// </summary>
    public string? UserOrgToken { get; set; }

    /// <summary>
    /// Dictionary of all exchanged tokens, keyed by target name.
    /// Used by the unified token exchange system.
    /// </summary>
    Dictionary<string, string> ExchangedTokens { get; }

    /// <summary>
    /// Get an exchanged token by target name.
    /// </summary>
    string? GetExchangedToken(string targetName);
}

public class RequestContext : IRequestContext
{
    private string _sshCaToken;
    private string _idpToken;
    private string? _userOrgToken;

    public long AdaptorUserId { get; set; } 
    public string UserName { get; set; }
    public string Email { get; set; }
    public string UserInfo { get; set; }

    /// <summary>
    /// SSH CA token – backed by ExchangedTokens["sshca"] for backward compatibility.
    /// </summary>
    public string SshCaToken
    {
        get => GetExchangedToken("sshca") ?? _sshCaToken;
        set { _sshCaToken = value; if (value != null) ExchangedTokens["sshca"] = value; }
    }

    /// <summary>
    /// IdP / FIP token – backed by ExchangedTokens["idp"] or ExchangedTokens["fip"] for backward compatibility.
    /// </summary>
    public string IdpToken
    {
        get => GetExchangedToken("idp") ?? GetExchangedToken("fip") ?? _idpToken;
        set { _idpToken = value; if (value != null) ExchangedTokens["idp"] = value; }
    }

    /// <summary>
    /// UserOrg token – backed by ExchangedTokens["userorg"] if present.
    /// </summary>
    public string? UserOrgToken
    {
        get => GetExchangedToken("userorg") ?? _userOrgToken;
        set { _userOrgToken = value; if (value != null) ExchangedTokens["userorg"] = value; }
    }

    public string LEXISToken { get; set; }

    /// <inheritdoc />
    public Dictionary<string, string> ExchangedTokens { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public string? GetExchangedToken(string targetName)
        => ExchangedTokens.TryGetValue(targetName, out var token) ? token : null;
}

public interface IHttpContextKeys
{
    Task<AdaptorUser> Authorize(ISshCertificateAuthorityService sshCertificateAuthorityService, IUserOrgService userOrgService, IExpirioService expirioService);
    Task<string> ExchangeSshCaToken(string tokenExchangeAddress, HttpClient httpClient);
    
    IRequestContext Context { get;  }
}

public class HttpContextKeys : IHttpContextKeys
{
    private readonly IRequestContext _context;
    public IRequestContext Context => _context;
    private readonly ILogger _logger;

    public HttpContextKeys(IRequestContext context, ILoggerFactory loggerFactory)
    {
        _context = context;
        _logger = loggerFactory.CreateLogger("HEAppE.BusinessLogicTier.AuthMiddleware.HttpContextKeys");
    }

    public async Task<AdaptorUser> Authorize(ISshCertificateAuthorityService sshCertificateAuthorityService, IUserOrgService userOrgService, IExpirioService expirioService)
    {
        _logger.LogInformation("[Authorize] Starting UserOrg authorization flow.");

        using var unitOfWork = UnitOfWorkFactory.GetUnitOfWorkFactory().CreateUnitOfWork(_logger);
        var userLogic = LogicFactory.GetLogicFactory().CreateUserAndLimitationManagementLogic(unitOfWork, userOrgService, sshCertificateAuthorityService, this, expirioService, _logger);
        AdaptorUser user = null;
        try
        {
            var effectiveUserOrgToken = Context.GetExchangedToken("userorg");
            if (LexisAuthenticationConfiguration.UseBearerAuth)
            {
                _logger.LogInformation("[Authorize] Using Bearer authentication for Lexis.");
                user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
                {
                    OpenIdLexisAccessToken = effectiveUserOrgToken ?? Context.LEXISToken
                });
            }
            else if (JwtTokenIntrospectionConfiguration.IsEnabled)
            {
                bool useLexisToken = JwtTokenIntrospectionConfiguration.LexisTokenFlowConfiguration.IsEnabled;
                _logger.LogInformation($"[Authorize] Using JWT introspection. LexisTokenFlowEnabled: {useLexisToken}");
                user = await userLogic.HandleTokenAsApiKeyAuthenticationAsync(new LexisCredentials
                {
                    OpenIdLexisAccessToken = effectiveUserOrgToken ?? (useLexisToken ? Context.LEXISToken : Context.IdpToken)
                });
            }
            
            if(user != null)
            {
                _logger.LogInformation($"[Authorize] Success. User: {user.Username}:{user.Email} (ID: {user.Id})");
                _context.AdaptorUserId = user.Id;
                _context.UserName = user.Username;
                _context.Email = user.Email;
                _context.UserInfo = $"{user.Username}:{user.Email}";
            }
            else
            {
                _logger.LogWarning("[Authorize] Authorization returned null user.");
            }
            
            return user;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Authorize] Exception occurred during authorization.");
            throw;
        }
    }

    public async Task<string> ExchangeSshCaToken(string tokenExchangeAddress, HttpClient httpClient)
    {
        _logger.LogInformation($"[SshCaExchange Request] URL: {tokenExchangeAddress}");
        var clientId = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientId;
        var clientSecret = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.ClientSecret;

        var authHeaderValue = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeaderValue);

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = JwtTokenIntrospectionConfiguration.TokenExchangeConfiguration.GrantType,
            ["subject_token"] = Context.IdpToken,
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
               _logger.LogError($"[SshCaExchange Response] Error: {response.StatusCode}");
               throw new ExternalException($"Token exchange service returned {response.StatusCode}.") { ServiceName = "KeycloakTokenExchange" };
            }

            _logger.LogDebug($"[SshCaExchange Response] Success: {response.StatusCode}");

            try
            {
                var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(content, new System.Text.Json.JsonSerializerOptions
                {
                  PropertyNameCaseInsensitive = true
                });
                
                _context.SshCaToken = tokenResponse.AccessToken;
                return tokenResponse.AccessToken;
            }
            catch (System.Text.Json.JsonException ex)
            {
                _logger.LogError(ex, $"[SshCaExchange] Failed to deserialize token response.");
                throw new ExternalException($"Token exchange service returned invalid JSON format: {ex.Message}") { ServiceName = "KeycloakTokenExchange" };
            }
        }
        catch (ExternalException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SshCaExchange] Failed to exchange SSH CA token.");
            throw new ExternalException("Internal error during SSH CA token exchange.", ex) { ServiceName = "KeycloakTokenExchange" };
        }
    }

    private void LogRequestDetails(string url, Dictionary<string, string> form)
    {
        _logger.LogDebug($"[SshCaExchange Details] URL: {url}, GrantType: {form["grant_type"]}, Audience: {form["audience"]}");
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
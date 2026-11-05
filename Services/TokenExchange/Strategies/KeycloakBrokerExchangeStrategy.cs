using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.TokenExchange.Strategies;

public class KeycloakBrokerExchangeStrategy : ITokenExchangeStrategy
{
    private readonly Rfc8693ExchangeStrategy _rfc8693Strategy;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeycloakBrokerExchangeStrategy> _logger;

    public string StrategyName => "KeycloakBroker";

    public KeycloakBrokerExchangeStrategy(
        Rfc8693ExchangeStrategy rfc8693Strategy,
        IHttpClientFactory httpClientFactory,
        ILogger<KeycloakBrokerExchangeStrategy> logger)
    {
        _rfc8693Strategy = rfc8693Strategy;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<TokenExchangeResult> ExchangeAsync(
        TokenExchangeTargetConfiguration target,
        string subjectToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Perform RFC 8693 token exchange to get intermediate token
            var rfcResult = await _rfc8693Strategy.ExchangeAsync(target, subjectToken, cancellationToken);
            if (!rfcResult.Success || string.IsNullOrEmpty(rfcResult.AccessToken))
            {
                _logger.LogError("Step 1 of Keycloak Broker exchange failed for target {Target}. Error: {Error}", target.Name, rfcResult.Error);
                return TokenExchangeResult.Failed(target.Name, $"Intermediate token exchange failed: {rfcResult.Error}");
            }

            var intermediateToken = rfcResult.AccessToken;

            // Step 2: Retrieve broker token
            var authority = target.Authority;
            if (string.IsNullOrEmpty(authority))
            {
                authority = JwtTokenIntrospectionConfiguration.Authority;
            }

            if (string.IsNullOrEmpty(authority))
            {
                return TokenExchangeResult.Failed(target.Name, "Authority is required for Keycloak Broker exchange.");
            }

            if (string.IsNullOrEmpty(target.BrokerAlias))
            {
                return TokenExchangeResult.Failed(target.Name, "BrokerAlias is required for Keycloak Broker exchange.");
            }

            var brokerEndpoint = $"{authority.TrimEnd('/')}/broker/{target.BrokerAlias}/token";
            var client = _httpClientFactory.CreateClient("TokenExchangeClient");

            using var request = new HttpRequestMessage(HttpMethod.Get, brokerEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", intermediateToken);

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Step 2 of Keycloak Broker exchange failed for target {Target}. Status: {Status}. Response: {Response}", target.Name, response.StatusCode, content);
                return TokenExchangeResult.Failed(target.Name, $"HTTP {response.StatusCode}: {content}");
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;

            string? finalToken = null;
            if (root.TryGetProperty("access_token", out var accessTokenProp))
            {
                finalToken = accessTokenProp.GetString();
            }

            if (string.IsNullOrEmpty(finalToken))
            {
                return TokenExchangeResult.Failed(target.Name, "Broker response did not contain an access_token.");
            }

            var result = TokenExchangeResult.Succeeded(target.Name, finalToken);
            
            if (root.TryGetProperty("expires_in", out var expiresInProp) && expiresInProp.TryGetInt32(out var expiresIn))
            {
                result.ExpiresIn = expiresIn;
            }

            if (root.TryGetProperty("issued_token_type", out var issuedTokenTypeProp))
            {
                result.IssuedTokenType = issuedTokenTypeProp.GetString();
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Keycloak Broker token exchange for target {Target}.", target.Name);
            return TokenExchangeResult.Failed(target.Name, ex.Message);
        }
    }
}

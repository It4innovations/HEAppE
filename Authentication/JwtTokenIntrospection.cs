

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HEAppE.ExternalAuthentication.Configuration;
using Microsoft.Extensions.Logging;

namespace HEAppE.Authentication;
/// <summary>
/// Implementation of JWT token introspection service
/// </summary>
public class JwtTokenIntrospectionService : IJwtTokenIntrospectionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JwtTokenIntrospectionService> _logger;
    private string? _introspectionEndpoint;

    public JwtTokenIntrospectionService(
        ILogger<JwtTokenIntrospectionService> logger)
    {
        logger.LogInformation("Initializing JwtTokenIntrospectionService with Authority: {Authority}",
            JwtTokenIntrospectionConfiguration.Authority);
        _httpClient = new HttpClient()
        {
            BaseAddress = new Uri(JwtTokenIntrospectionConfiguration.Authority)
        };
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("HEAppE Middleware (contact: support.heappe@it4i.cz)");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    public async Task<TokenIntrospectionResult> IntrospectTokenAsync(string token)
    {
        if(!JwtTokenIntrospectionConfiguration.IsEnabled)
        {
            _logger.LogWarning("JWT token introspection is disabled. Returning inactive result.");
            return new TokenIntrospectionResult { Active = false };
        }
        try
        {
            // Get introspection endpoint if not cached
            if (string.IsNullOrEmpty(_introspectionEndpoint))
            {
                _introspectionEndpoint = await GetIntrospectionEndpointAsync();
                if (string.IsNullOrEmpty(_introspectionEndpoint))
                {
                    _logger.LogError("Introspection endpoint is not configured or could not be retrieved.");
                    return new TokenIntrospectionResult { Active = false };
                }

                _logger.LogInformation("Using introspection endpoint: {IntrospectionEndpoint}", _introspectionEndpoint);
            }

            // Prepare introspection request
            var requestData = new List<KeyValuePair<string, string>>
            {
                new("token", token),
                new("token_type_hint", "access_token")
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _introspectionEndpoint)
            {
                Content = new FormUrlEncodedContent(requestData)
            };

            // Add Basic authentication header
            var authValue = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    $"{JwtTokenIntrospectionConfiguration.ClientId}:{JwtTokenIntrospectionConfiguration.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Token introspection successful");
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TokenIntrospectionResult>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result ?? new TokenIntrospectionResult { Active = false };
            }

            _logger.LogWarning("Token introspection failed with status: {StatusCode}", response.StatusCode);
            return new TokenIntrospectionResult { Active = false };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token introspection");
            return new TokenIntrospectionResult { Active = false };
        }
    }

    private async Task<string?> GetIntrospectionEndpointAsync()
    {
        _logger.LogInformation("Fetching introspection endpoint from well-known configuration");
        try
        {
            var wellKnownUrl =
                $"{JwtTokenIntrospectionConfiguration.Authority.TrimEnd('/')}/.well-known/openid-configuration";

            WellKnownResponse? response = await _httpClient.GetFromJsonAsync<WellKnownResponse>(wellKnownUrl);

            if (response == null)
            {
                _logger.LogError("Failed to deserialize well-known configuration from: {Url}", wellKnownUrl);
                return null;
            }
            return response.IntrospectionEndpoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting introspection endpoint");
            return null;
        }
    }
}

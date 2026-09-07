#pragma warning disable CS8625, CS8600, CS8603, CS8602, CS8604
using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using log4net;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.UserOrg;

public class UserOrgV1Service(IHttpClientFactory httpClientFactory) : IUserOrgService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private const string ClientName = "userOrgApi";

    private string BuildUrl(params string[] segments)
    {
        var cleanedSegments = segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim('/'));

        return string.Join("/", cleanedSegments);
    }

    public async Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken, string instanceId, ILogger logger)
    {
        string relativeUri = BuildUrl(
            LexisAuthenticationConfiguration.EndpointPrefix, 
            LexisAuthenticationConfiguration.ExtendedUserInfoEndpoint
        );
        logger.LogDebug($"[UserOrg v1 Request] GetUserInfo: {relativeUri}");
        var request = CreateRequest(logger, HttpMethod.Get, relativeUri, accessToken, instanceId);
        return await SendAsync<UserInfoExtendedModel>(request, logger);
    }

    public async Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier, string instanceId, ILogger logger)
    {
        string relativeUri = BuildUrl(
            LexisAuthenticationConfiguration.EndpointPrefix, 
            LexisAuthenticationConfiguration.CommandTemplatePermissions, 
            heappeInstanceIdentifier
        );
        logger.LogDebug($"[UserOrg v1 Request] GetPermissions: {relativeUri} for Instance: {heappeInstanceIdentifier}");
        var request = CreateRequest(logger, HttpMethod.Get, relativeUri, accessToken, instanceId);
        return await SendAsync<CommandTemplatePermissionsModel>(request, logger);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger)
    {
        if (!IsTemplateEnabledInLexis(permissions, clusterName, queueName, accountingString, commandTemplateName))
        {
            logger.LogWarning($"[UserOrg v1 Validation] Permission denied for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
            throw new UnauthorizedAccessException($"No LEXIS permissions for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
        }
        logger.LogDebug($"[UserOrg v1 Validation] Permission granted for Template:{commandTemplateName}");
    }

    public bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName)
    {
        return permissions.Permissions
        .Where(p => string.Equals(p.ProjectResource, accountingString, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.ClusterName, clusterName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(p.QueueName, queueName, StringComparison.OrdinalIgnoreCase))
        .Any(p => p.CommandTemplates.Any(ct => 
            string.Equals(ct.Name, templateName, StringComparison.OrdinalIgnoreCase) && ct.Enabled));
    }

    private HttpRequestMessage CreateRequest(ILogger logger, HttpMethod method, string relativeUri, string accessToken, string instanceId, object body = null)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Add("X-Api-Token", accessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        string version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
        request.Headers.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
        
        if (body != null) 
        {
            request.Content = JsonContent.Create(body);
            logger.LogDebug($"[UserOrg v1 Request Body] {JsonSerializer.Serialize(body)}");
        }
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, ILogger logger)
    {
        using var httpClient = _httpClientFactory.CreateClient(ClientName);
        logger.LogInformation($"[UserOrg v1 API] Sending {request.Method} request to {request.RequestUri}");
        
        try
        {
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogDebug($"[UserOrg v1 Response] Success ({response.StatusCode}). Body: {content}");
                try
                {
                    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, $"[UserOrg v1 API] Failed to deserialize JSON response. Content: {content}");
                    throw new AuthenticationTypeException("InvalidResponseFormat", "UserOrg") { Details = $"Expected JSON but received invalid format: {ex.Message}" };
                }
            }
            else
            {
                string traceId = TryGetTraceId(response, content);
                string traceIdPart = !string.IsNullOrEmpty(traceId) ? $"\nTrace ID: {traceId}" : "";
                string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}{traceIdPart}";
                logger.LogError($"[UserOrg v1 API Error] UserOrg API Error: {details}");

                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new AuthenticationTypeException("BadRequest", "UserOrg") { Details = details };
                    case HttpStatusCode.Unauthorized:
                        throw new AuthenticationTypeException("InvalidToken", "UserOrg") { Details = details };
                    case HttpStatusCode.NotFound:
                        throw new AuthenticationTypeException("NotFound", "UserOrg") { Details = details };
                    case HttpStatusCode.InternalServerError:
                        throw new AuthenticationTypeException("ServerError", "UserOrg") { Details = details };
                    case HttpStatusCode.BadGateway:
                        throw new AuthenticationTypeException("UpstreamError", "UserOrg") { Details = details };
                    default:
                        throw new AuthenticationTypeException("ExternalApiError", "UserOrg") { Details = details };
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"[UserOrg v1 API Timeout] Request to {request.RequestUri} timed out after the configured timeout.");
            throw new AuthenticationTypeException("ExternalApiTimeout", "UserOrg") { Details = $"The request to UserOrg API timed out. Technical details: {ex.Message}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[UserOrg v1 API Exception] Unexpected error during request to {request.RequestUri}");
            throw new AuthenticationTypeException("ExternalApiError", ex, "UserOrg")
            {
                ServiceName = "UserOrg",
                Details = $"Unexpected error during request to UserOrg API. Technical details: {ex.Message}"
            };
        }
    }

    private string TryGetTraceId(HttpResponseMessage response, string content)
    {
        var headersToCheck = new[] { "traceparent", "trace-id", "X-Trace-Id", "traceid", "request-id", "X-Request-Id", "correlation-id", "X-Correlation-Id" };
        foreach (var header in headersToCheck)
        {
            if (response.Headers.TryGetValues(header, out var values) && values.Any())
            {
                return values.First();
            }
            if (response.Content != null && response.Content.Headers.TryGetValues(header, out var contentValues) && contentValues.Any())
            {
                return contentValues.First();
            }
        }

        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    var jsonPropertiesToCheck = new[] { "traceId", "trace_id", "traceID", "traceparent", "requestId", "request_id", "requestID" };
                    foreach (var propName in jsonPropertiesToCheck)
                    {
                        if (root.TryGetProperty(propName, out var prop) && prop.ValueKind == JsonValueKind.String)
                        {
                            return prop.GetString();
                        }
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }
        }

        return null;
    }
}

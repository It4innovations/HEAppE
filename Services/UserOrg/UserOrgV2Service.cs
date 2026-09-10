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

public class UserOrgV2Service(IHttpClientFactory httpClientFactory) : IUserOrgService
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
            LexisAuthenticationConfiguration.ExtendedUserInfoV2Endpoint
        );
        logger.LogDebug($"[UserOrg v2 Request] GetUserInfo: {relativeUri}");
        var request = CreateRequest(logger, HttpMethod.Get, relativeUri, accessToken, instanceId);
        return await SendAsync<UserInfoExtendedModel>(request, logger);
    }

    public async Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier, string instanceId, ILogger logger)
    {
        string relativeUri = BuildUrl(
            LexisAuthenticationConfiguration.EndpointPrefix, 
            LexisAuthenticationConfiguration.CommandTemplatePermissionsV2Endpoint, 
            heappeInstanceIdentifier
        );
        logger.LogDebug($"[UserOrg v2 Request] GetPermissions: {relativeUri} for Instance: {heappeInstanceIdentifier}");
        var request = CreateRequest(logger, HttpMethod.Get, relativeUri, accessToken, instanceId);
        return await SendAsync<CommandTemplatePermissionsModel>(request, logger);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger)
    {
        if (!IsTemplateEnabledInLexis(permissions, clusterName, queueName, accountingString, commandTemplateName))
        {
            logger.LogWarning($"[UserOrg v2 Validation] Permission denied for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
            throw new UnauthorizedAccessException($"No LEXIS permissions for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
        }
        logger.LogDebug($"[UserOrg v2 Validation] Permission granted for Template:{commandTemplateName}");
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
            logger.LogDebug($"[UserOrg v2 Request Body] {JsonSerializer.Serialize(body)}");
        }
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, ILogger logger)
    {
        using var httpClient = _httpClientFactory.CreateClient(ClientName);
        logger.LogInformation($"[UserOrg v2 API] Sending {request.Method} request to {request.RequestUri}");
        
        try
        {
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogDebug($"[UserOrg v2 Response] Success ({response.StatusCode}). Body: {content}");
                try
                {
                    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, $"[UserOrg v2 API] Failed to deserialize JSON response. Content: {content}");
                    throw new AuthenticationTypeException("InvalidResponseFormat", "UserOrg") { Details = $"Expected JSON but received invalid format: {ex.Message}" };
                }
            }
            else
            {
                string problemCode = null;
                string problemTitle = null;

                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(content);
                        var root = jsonDoc.RootElement;
                        if (root.ValueKind == JsonValueKind.Object)
                        {
                            if (root.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
                                problemCode = codeProp.GetString();
                            if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
                                problemTitle = titleProp.GetString();
                        }
                    }
                    catch
                    {
                        // Fallback if not ProblemDetails JSON
                    }
                }

                string traceId = TryGetTraceId(response, content);
                string traceIdPart = !string.IsNullOrEmpty(traceId) ? $"\nTrace ID: {traceId}" : "";
                string codePart = !string.IsNullOrEmpty(problemCode) ? $"\nProblem Code: {problemCode}" : "";
                string titlePart = !string.IsNullOrEmpty(problemTitle) ? $"\nTitle: {problemTitle}" : "";
                string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.{codePart}{titlePart}\nContent: {content}{traceIdPart}";
                logger.LogError($"[UserOrg v2 API Error] UserOrg API Error: {details}");

                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        throw new AuthenticationTypeException("BadRequest", "UserOrg") { Details = details };
                    case HttpStatusCode.Unauthorized:
                        throw new AuthenticationTypeException("InvalidToken", "UserOrg") { Details = details };
                    case HttpStatusCode.Forbidden:
                        throw new AuthenticationTypeException("Forbidden", "UserOrg") { Details = details };
                    case HttpStatusCode.NotFound:
                        throw new AuthenticationTypeException("NotFound", "UserOrg") { Details = details };
                    case HttpStatusCode.MethodNotAllowed:
                        throw new AuthenticationTypeException("MethodNotAllowed", "UserOrg") { Details = details };
                    case HttpStatusCode.PreconditionFailed:
                        throw new AuthenticationTypeException("PreconditionFailed", "UserOrg") { Details = details };
                    case HttpStatusCode.InternalServerError:
                        throw new AuthenticationTypeException("ServerError", "UserOrg") { Details = details };
                    case HttpStatusCode.BadGateway:
                        throw new AuthenticationTypeException("UpstreamError", "UserOrg") { Details = details };
                    default:
                        throw new AuthenticationTypeException("ExternalApiError", "UserOrg") { Details = details };
                }
            }
        }
        catch (AuthenticationTypeException)
        {
            throw;
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, $"[UserOrg v2 API Timeout] Request to {request.RequestUri} timed out after the configured timeout.");
            throw new AuthenticationTypeException("ExternalApiTimeout", "UserOrg") { Details = $"The request to UserOrg API v2 timed out. Technical details: {ex.Message}" };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[UserOrg v2 API Exception] Unexpected error during request to {request.RequestUri}");
            throw new AuthenticationTypeException("ExternalApiError", ex, "UserOrg")
            {
                ServiceName = "UserOrg",
                Details = $"Unexpected error during request to UserOrg API v2. Technical details: {ex.Message}"
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

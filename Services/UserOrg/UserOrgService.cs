#pragma warning disable CS8625, CS8600, CS8603, CS8602, CS8604
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Net.Http.Headers;
using System.Text.Json;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
//using HEAppE.HpcConnectionFramework.Configuration;
using Microsoft.Extensions.Logging;
using log4net;

namespace HEAppE.Services.UserOrg;

public interface IUserOrgService
{
    Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken, string instanceId, ILogger logger);
    Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier, string instanceId, ILogger logger);
    void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger);
    bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName);
}

public class UserOrgService(IHttpClientFactory httpClientFactory) : IUserOrgService
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
        logger.LogDebug($"[UserOrg Request] GetUserInfo: {relativeUri}");
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
        logger.LogDebug($"[UserOrg Request] GetPermissions: {relativeUri} for Instance: {heappeInstanceIdentifier}");
        var request = CreateRequest(logger, HttpMethod.Get, relativeUri, accessToken, instanceId);
        return await SendAsync<CommandTemplatePermissionsModel>(request, logger);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger)
    {
        if (!IsTemplateEnabledInLexis(permissions, clusterName, queueName, accountingString, commandTemplateName))
        {
            logger.LogWarning($"[UserOrg Validation] Permission denied for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
            throw new UnauthorizedAccessException($"No LEXIS permissions for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
        }
        logger.LogDebug($"[UserOrg Validation] Permission granted for Template:{commandTemplateName}");
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
        
        //string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
        string version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
        
        request.Headers.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
        
        if (body != null) 
        {
            request.Content = JsonContent.Create(body);
            logger.LogDebug($"[UserOrg Request Body] {JsonSerializer.Serialize(body)}");
        }
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, ILogger logger)
    {
        using var httpClient = _httpClientFactory.CreateClient(ClientName);
        logger.LogInformation($"[UserOrg API] Sending {request.Method} request to {request.RequestUri}");
        
        try
        {
            using var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                logger.LogDebug($"[UserOrg Response] Success ({response.StatusCode}). Body: {content}");
                try
                {
                    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    logger.LogError(ex, $"[UserOrg API] Failed to deserialize JSON response. Content: {content}");
                    throw new AuthenticationTypeException("InvalidResponseFormat", "UserOrg") { Details = $"Expected JSON but received invalid format: {ex.Message}" };
                }
            }
            else
            {
                string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
                logger.LogError($"[UserOrg API Error] UserOrg API Error: {details}");

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
        catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
        {
            logger.LogError($"[UserOrg API Timeout] Request to {request.RequestUri} timed out after the configured timeout.");
            throw new AuthenticationTypeException("ExternalApiTimeout", "UserOrg") { Details = "The request to UserOrg API timed out." };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[UserOrg API Exception] Unexpected error during request to {request.RequestUri}");
            throw;
        }
    }
}
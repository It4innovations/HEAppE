#pragma warning disable CS8625, CS8600, CS8603, CS8602, CS8604
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Net.Http.Headers;
using System.Text.Json;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using HEAppE.HpcConnectionFramework.Configuration;
using log4net;

namespace HEAppE.Services.UserOrg;

public interface IUserOrgService
{
    Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken);
    Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier);
    void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName);
    bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName);
}

public class UserOrgService(IHttpClientFactory httpClientFactory) : IUserOrgService
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string ClientName = "userOrgApi";

    private string BuildUrl(params string[] segments)
    {
        var cleanedSegments = segments
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim('/'));

        return string.Join("/", cleanedSegments);
    }

    public async Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken)
    {
        string relativeUri = BuildUrl(
            LexisAuthenticationConfiguration.EndpointPrefix, 
            LexisAuthenticationConfiguration.ExtendedUserInfoEndpoint
        );
        _log.Debug($"[UserOrg Request] GetUserInfo: {relativeUri}");
        var request = CreateRequest(HttpMethod.Get, relativeUri, accessToken);
        return await SendAsync<UserInfoExtendedModel>(request);
    }

    public async Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier)
    {
        string relativeUri = BuildUrl(
            LexisAuthenticationConfiguration.EndpointPrefix, 
            LexisAuthenticationConfiguration.CommandTemplatePermissions, 
            heappeInstanceIdentifier
        );
        _log.Debug($"[UserOrg Request] GetPermissions: {relativeUri} for Instance: {heappeInstanceIdentifier}");
        var request = CreateRequest(HttpMethod.Get, relativeUri, accessToken);
        return await SendAsync<CommandTemplatePermissionsModel>(request);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName)
    {
        if (!IsTemplateEnabledInLexis(permissions, clusterName, queueName, accountingString, commandTemplateName))
        {
            _log.Warn($"[UserOrg Validation] Permission denied for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
            throw new UnauthorizedAccessException($"No LEXIS permissions for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
        }
        _log.Debug($"[UserOrg Validation] Permission granted for Template:{commandTemplateName}");
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

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri, string accessToken, object body = null)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Add("X-Api-Token", accessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
        string version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
        
        request.Headers.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
        
        if (body != null) 
        {
            request.Content = JsonContent.Create(body);
            _log.Debug($"[UserOrg Request Body] {JsonSerializer.Serialize(body)}");
        }
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        using var httpClient = _httpClientFactory.CreateClient(ClientName);
        _log.Info($"[UserOrg API] Sending {request.Method} request to {request.RequestUri}");
        
        using var response = await httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _log.Debug($"[UserOrg Response] Success ({response.StatusCode}). Body: {content}");
            try
            {
                return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _log.Error($"[UserOrg API] Failed to deserialize JSON response. Content: {content}", ex);
                throw new AuthenticationTypeException("InvalidResponseFormat", $"Expected JSON but received invalid format: {ex.Message}");
            }
        }
        else
        {
            string details = $"Status code: {response.StatusCode}.\nReason: {response.ReasonPhrase}.\nContent: {content}";
            _log.Error($"[UserOrg API Error] Details: {details}");

            switch (response.StatusCode)
            {
                case HttpStatusCode.BadRequest:
                    throw new AuthenticationTypeException("BadRequest", details);
                case HttpStatusCode.Unauthorized:
                    throw new AuthenticationTypeException("InvalidToken", details);
                case HttpStatusCode.NotFound:
                    throw new AuthenticationTypeException("NotFound", details);
                case HttpStatusCode.InternalServerError:
                    throw new AuthenticationTypeException("ServerError", details);
                case HttpStatusCode.BadGateway:
                    throw new AuthenticationTypeException("UpstreamError", details);
                default:
                    throw new AuthenticationTypeException("ExternalApiError", details);
            }
        }
    }
}
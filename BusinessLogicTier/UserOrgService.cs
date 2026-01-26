using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using HEAppE.HpcConnectionFramework.Configuration;
using log4net;

namespace HEAppE.BusinessLogicTier;

public interface IUserOrgService
{
    Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken);
    Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier);
    void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName);
    bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName);
}
public class UserOrgService(IHttpClientFactory httpClientFactory) : IUserOrgService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient(ClientName);
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
        var request = CreateRequest(HttpMethod.Get, relativeUri, accessToken);
        return await SendAsync<CommandTemplatePermissionsModel>(request);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName)
    {
        if (!IsTemplateEnabledInLexis(permissions, clusterName, queueName, accountingString, commandTemplateName))
        {
            throw new UnauthorizedAccessException($"No LEXIS permissions for Cluster:{clusterName}, Queue:{queueName}, Project:{accountingString}, Template:{commandTemplateName}");
        }
    }

    public bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName)
    {
        var permission = permissions.Permissions.FirstOrDefault(p =>
            string.Equals(p.ClusterName, clusterName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.QueueName, queueName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.ProjectResource, accountingString, StringComparison.OrdinalIgnoreCase));

        if (permission == null) return false;

        var template = permission.CommandTemplates.FirstOrDefault(ct => 
            string.Equals(ct.Name, templateName, StringComparison.OrdinalIgnoreCase));

        return template != null && template.Enabled;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri, string accessToken, object body = null)
    {
        var request = new HttpRequestMessage(method, relativeUri);
        request.Headers.Add("X-Api-Token", accessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        string instanceId = HPCConnectionFrameworkConfiguration.ScriptsSettings.InstanceIdentifierPath;
        string version = (GlobalContext.Properties["instanceVersion"] ?? "unknown").ToString();
        request.Headers.UserAgent.ParseAdd($"HEAppE-{instanceId}/{version}");
        if (body != null) request.Content = JsonContent.Create(body);
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        try
        {
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationTypeException("InvalidToken");
            }
            return await response.Content.ReadFromJsonAsync<T>();
        }
        catch (Exception)
        {
            throw new AuthenticationTypeException("InvalidToken");
        }
    }
}
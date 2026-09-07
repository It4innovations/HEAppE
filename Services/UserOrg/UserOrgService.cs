#pragma warning disable CS8625, CS8600, CS8603, CS8602, CS8604
using System;
using System.Net.Http;
using System.Threading.Tasks;
using HEAppE.Exceptions.External;
using HEAppE.ExternalAuthentication.Configuration;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.UserOrg;

public interface IUserOrgService
{
    Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken, string instanceId, ILogger logger);
    Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier, string instanceId, ILogger logger);
    void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger);
    bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName);
}

public class UserOrgService : IUserOrgService
{
    private readonly UserOrgV1Service _v1Service;
    private readonly UserOrgV2Service _v2Service;

    public UserOrgService(IHttpClientFactory httpClientFactory)
    {
        _v1Service = new UserOrgV1Service(httpClientFactory);
        _v2Service = new UserOrgV2Service(httpClientFactory);
    }

    private IUserOrgService ResolveService()
    {
        string apiVersion = LexisAuthenticationConfiguration.ApiVersion?.ToLowerInvariant();
        if (apiVersion == "v1")
        {
            return _v1Service;
        }
        return _v2Service;
    }

    public async Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken, string instanceId, ILogger logger)
    {
        string apiVersion = LexisAuthenticationConfiguration.ApiVersion?.ToLowerInvariant();
        if (apiVersion == "auto")
        {
            try
            {
                logger.LogDebug("[UserOrg Strategy] Auto mode: trying v2 endpoint first...");
                return await _v2Service.GetUserInfoAsync(accessToken, instanceId, logger);
            }
            catch (AuthenticationTypeException ex) when (ex.Details?.Contains("NotFound") == true || ex.Details?.Contains("404") == true)
            {
                logger.LogWarning("[UserOrg Strategy] Auto mode: v2 returned NotFound (404), falling back to v1...");
                return await _v1Service.GetUserInfoAsync(accessToken, instanceId, logger);
            }
        }

        return await ResolveService().GetUserInfoAsync(accessToken, instanceId, logger);
    }

    public async Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier, string instanceId, ILogger logger)
    {
        string apiVersion = LexisAuthenticationConfiguration.ApiVersion?.ToLowerInvariant();
        if (apiVersion == "auto")
        {
            try
            {
                logger.LogDebug("[UserOrg Strategy] Auto mode: trying v2 permissions endpoint first...");
                return await _v2Service.GetCommandTemplatePermissionsAsync(accessToken, heappeInstanceIdentifier, instanceId, logger);
            }
            catch (AuthenticationTypeException ex) when (ex.Details?.Contains("NotFound") == true || ex.Details?.Contains("404") == true)
            {
                logger.LogWarning("[UserOrg Strategy] Auto mode: v2 permissions returned NotFound (404), falling back to v1...");
                return await _v1Service.GetCommandTemplatePermissionsAsync(accessToken, heappeInstanceIdentifier, instanceId, logger);
            }
        }

        return await ResolveService().GetCommandTemplatePermissionsAsync(accessToken, heappeInstanceIdentifier, instanceId, logger);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger)
    {
        ResolveService().ValidatePermissions(permissions, clusterName, queueName, accountingString, commandTemplateName, logger);
    }

    public bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName)
    {
        return ResolveService().IsTemplateEnabledInLexis(permissions, clusterName, queueName, accountingString, templateName);
    }
}
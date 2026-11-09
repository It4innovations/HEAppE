using System.Threading.Tasks;
using HEAppE.ExternalAuthentication.DTO.LexisAuth;
using Microsoft.Extensions.Logging;

namespace HEAppE.Services.UserOrg;

/// <summary>
/// A no-op implementation of <see cref="IUserOrgService"/> used when UserOrg integration is disabled
/// or not configured, avoiding external HTTP calls and unnecessary errors.
/// </summary>
public class NullUserOrgService : IUserOrgService
{
    public Task<UserInfoExtendedModel> GetUserInfoAsync(string accessToken, string instanceId, ILogger logger)
    {
        logger.LogDebug("[NullUserOrgService] UserOrg is not configured or disabled. Returning empty UserInfoExtendedModel.");
        return Task.FromResult(new UserInfoExtendedModel());
    }

    public Task<CommandTemplatePermissionsModel> GetCommandTemplatePermissionsAsync(string accessToken, string heappeInstanceIdentifier, string instanceId, ILogger logger)
    {
        return Task.FromResult<CommandTemplatePermissionsModel>(null);
    }

    public void ValidatePermissions(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string commandTemplateName, ILogger logger)
    {
        // No-op: do not restrict command template permissions when UserOrg is not used
    }

    public bool IsTemplateEnabledInLexis(CommandTemplatePermissionsModel permissions, string clusterName, string queueName, string accountingString, string templateName)
    {
        // Permissive: allow template if UserOrg is disabled
        return true;
    }
}

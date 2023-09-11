using HEAppE.ExtModels.ClusterInformation.Models;
using HEAppE.ExtModels.Management.Models;

namespace HEAppE.ServiceTier.Management
{
    public interface IManagementService
    {
        CommandTemplateExt CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        PublicKeyExt CreateSecureShellKey(string username, string[] accountingStrings, string sessionCode);
        PublicKeyExt RecreateSecureShellKey(string username, string publicKey, string sessionCode);
        string RemoveSecureShellKey(string publicKey, string sessionCode);
        CommandTemplateExt ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript, string sessionCode);
        string RemoveCommandTemplate(long commandTemplateId, string sessionCode);
    }
}

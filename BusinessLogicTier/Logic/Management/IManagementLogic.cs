using HEAppE.DomainObjects.JobManagement;
using HEAppE.DomainObjects.Management;

namespace HEAppE.BusinessLogicTier.Logic.Management
{
    public interface IManagementLogic
    {
        CommandTemplate CreateCommandTemplate(long genericCommandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        void RemoveCommandTemplate(long commandTemplateId);
        CommandTemplate ModifyCommandTemplate(long commandTemplateId, string name, long projectId, string description, string code, string executableFile, string preparationScript);
        SecureShellKey CreateSecureShellKey(string username, long[] projects);
        SecureShellKey RecreateSecureShellKey(string username, string publicKey);
        string RemoveSecureShellKey(string publicKey);
    }
}
